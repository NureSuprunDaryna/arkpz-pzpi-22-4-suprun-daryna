using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.ViewModels.AppUser;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (model == null)
            {
                _logger.LogError("Register - model is null");
                return BadRequest("Invalid registration data.");
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                _logger.LogWarning("Register - user with email {Email} already exists.", model.Email);
                return Conflict("An account with this email already exists.");
            }

            var existingUserByNickname = _userManager.Users.FirstOrDefault(u => u.UserName == model.UserName);
            if (existingUserByNickname != null)
            {
                _logger.LogWarning("Register - user with nickname {UserName} already exists.", model.UserName);
                return Conflict("An account with this nickname already exists.");
            }

            var newUser = new AppUser();
            newUser.MapFrom(model);
            newUser.Joined = DateTime.UtcNow;

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (!result.Succeeded)
            {
                _logger.LogError("Register - failed to create user.");
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Register - Error: {ErrorMessage}", error.Description);
                }
                return BadRequest("Failed to register user.");
            }

            var roleResult = await _userManager.AddToRoleAsync(newUser, newUser.Role.ToString());
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                _logger.LogError("Register - failed to assign role to user {UserName}.", model.UserName);
                return BadRequest("Failed to assign role to user.");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(newUser);

            _logger.LogInformation("Generated confirmation token: {Token}", token);

            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = newUser.Id, token },
                protocol: HttpContext.Request.Scheme);

            if (string.IsNullOrEmpty(callbackUrl))
            {
                _logger.LogError("Register - Failed to generate confirmation URL.");
                return BadRequest("Failed to generate email confirmation link.");
            }

            var emailResult = await _emailService.SendRegistrationConfirmationAsync(newUser, callbackUrl);
            if (!emailResult.IsSuccessful)
            {
                _logger.LogError("Register - Failed to send confirmation email.");
                return BadRequest("Failed to send confirmation email.");
            }

            _logger.LogInformation("User {Email} registered successfully with nickname {UserName}, confirmation email sent.", model.Email, model.UserName);
            return Ok("Registration successful. Please check your email to confirm your account.");
        }


        [HttpGet("externallogin/{provider}")]
        public IActionResult ExternalLogin(string provider)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account");

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return new ChallengeResult(provider, properties);
        }

        [HttpPost("ExternalLoginCallback")]
        public async Task<IActionResult> ExternalLoginCallback(string remoteError = null)
        {
            if (remoteError != null)
            {
                _logger.LogError("Error from external provider: {errorMessage}", remoteError);

                return BadRequest();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                _logger.LogError("Error loading external login information.");

                return BadRequest();
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync
                (info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                var user = await _userManager.GetUserAsync(User);

                return Ok(user.Id);
            }

            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);

                if (email != null)
                {
                    var user = await _userManager.FindByEmailAsync(email);

                    if (user == null)
                    {
                        var viewModel = new RegisterModel()
                        {
                            FirstName = info.Principal.FindFirstValue(ClaimTypes.GivenName),
                            LastName = info.Principal.FindFirstValue(ClaimTypes.Surname),
                            Email = info.Principal.FindFirstValue(ClaimTypes.Email)
                        };

                        var newUser = CreateUser(viewModel);

                        newUser.EmailConfirmed = true;

                        var newUserResponse = await _userManager.CreateAsync(newUser);

                        if (newUserResponse.Succeeded)
                        {
                            var roleResult = await _userManager
                                .AddToRoleAsync(newUser, user.Role.ToString());

                            if (!roleResult.Succeeded)
                            {
                                await _userManager.DeleteAsync(newUser);

                                return BadRequest();
                            }

                            await _signInManager.SignInAsync(newUser, isPersistent: false);

                            return Ok(newUser.Id);
                        }
                        else
                        {
                            _logger.LogError("Failed to register user!");

                            var errorMessage = new StringBuilder();

                            foreach (var error in newUserResponse.Errors)
                            {
                                errorMessage.AppendLine(error.Description);

                                _logger.LogError("Error: {errorMessage}", error.Description);
                            }

                            return BadRequest(errorMessage);
                        }
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return Ok(user.Id);
                }
                else
                {
                    _logger.LogError("Error loading external login information - email was null.");

                    return BadRequest();
                }
            }
        }

        private AppUser CreateUser(RegisterModel viewModel)
        {
            var newUser = new AppUser();
            newUser.MapFrom(viewModel);
            newUser.Role = "User";
            newUser.Joined = DateTime.UtcNow;

            return newUser;
        }

        [HttpPut("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string nickname, string code)
        {
            if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("Failed to confirm user email - code or nickname was null");
                return BadRequest();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == nickname);

            if (user == null)
            {
                _logger.LogWarning("Failed to confirm email of user {nickname} - user not found!", nickname);
                return BadRequest();
            }

            var result = await _userManager.ConfirmEmailAsync(user, code);

            if (result.Succeeded)
            {
                return Ok();
            }
            else
            {
                _logger.LogError("Failed to confirm email of user {nickname}!", nickname);
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("Error: {errorMessage}", error.Description);
                }
                return BadRequest();
            }
        }

        private string CreateCallBackUrl
            (string controllerName, string actionName, object routeValues)
        {
            if (string.IsNullOrEmpty(controllerName) || string.IsNullOrEmpty(actionName))
            {
                return string.Empty;
            }

            if (routeValues == null)
            {
                return string.Empty;
            }

            var callbackUrl = Url.Action(
                actionName,
                controllerName,
                routeValues,
                protocol: HttpContext.Request.Scheme);

            return callbackUrl;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel viewModel)
        {
            if (string.IsNullOrWhiteSpace(viewModel.Nickname) || string.IsNullOrWhiteSpace(viewModel.Password))
            {
                _logger.LogError("Login failed - nickname or password was null or empty.");
                return BadRequest("Invalid login credentials.");
            }

            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.UserName == viewModel.Nickname);

            if (user == null)
            {
                _logger.LogWarning("Login failed - user with nickname {UserName} not found.", viewModel.Nickname);
                return BadRequest("User not found.");
            }

            var passwordCheckResult = await _userManager.CheckPasswordAsync(user, viewModel.Password);

            if (!passwordCheckResult)
            {
                _logger.LogWarning("Login failed - incorrect password for nickname {UserName}.", viewModel.Nickname);
                return BadRequest("Incorrect password.");
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Login failed - email for user with nickname {UserName} not confirmed.", viewModel.Nickname);

                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var callbackUrl = CreateCallBackUrl("Account", "ConfirmEmail", new { userId = user.Id, code });

                if (string.IsNullOrEmpty(callbackUrl))
                {
                    _logger.LogError("Failed to generate confirmation link for user {UserName}.", viewModel.Nickname);
                    return BadRequest("Failed to generate confirmation link.");
                }

                var emailResult = await _emailService.SendRegistrationConfirmationAsync(user, callbackUrl);

                if (!emailResult.IsSuccessful)
                {
                    _logger.LogError("Failed to send confirmation email to user {UserName}.", viewModel.Nickname);
                    return BadRequest("Failed to send confirmation email.");
                }

                return Unauthorized("Email not confirmed. Please check your email.");
            }

            HttpContext.Session.SetString("UserId", user.Id); // Збереження userId у сесії

            _logger.LogInformation("User with nickname {UserName} logged in successfully.", viewModel.Nickname);
            var token = GenerateJwtToken(user);

            return Ok(user);
        }

        private string GenerateJwtToken(AppUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("YourSuperLongSuperSecureSecretKey2025"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "https://elixir_nocturne.com",
                audience: "https://elixir_nocturne.com",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );
            _logger.LogInformation("Generated confirmation token: {Token}", token);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Failed to get profile - user not logged in.");
                return Unauthorized("You are not logged in.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Failed to get profile - user with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            var profile = new UserProfileModel
            {
                Nickname = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Bio = user.Bio,
                Email = user.Email,
                Joined = user.Joined,
                ElixirsCount = user.Elixirs.Count
            };

            _logger.LogInformation("Profile for user {UserName} retrieved successfully.", user.UserName);
            return Ok(profile);
        }

        [HttpPut("profile/update")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateModel model)
        {
            var userId = HttpContext.Session.GetString("UserId"); // Отримання userId з сесії

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Failed to update profile - user not logged in.");
                return Unauthorized("You are not logged in.");
            }

            if (model == null)
            {
                _logger.LogError("UpdateProfile - update model is null");
                return BadRequest("Profile data is null");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("Failed to update profile - user with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            user.MapFrom(model);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update profile for user {UserName}.", user.UserName);
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Error: {Error}", error.Description);
                }
                return BadRequest("Failed to update profile.");
            }

            _logger.LogInformation("User profile for {UserName} updated successfully.", user.UserName);
            return Ok("Profile updated successfully.");
        }

        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdministrator([FromBody] RegisterAdminModel model)
        {
            if (model == null)
            {
                _logger.LogError("RegisterAdmin - model was not received");
                return BadRequest("Invalid model");
            }

            // Validate role
            if (model.Role != "AdminDB" && model.Role != "Admin")
            {
                _logger.LogError("RegisterAdmin - Incorrect role provided");
                return BadRequest("Invalid role");
            }

            try
            {
                // Check if email is already taken
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("RegisterAdmin - Email {Email} is already in use", model.Email);
                    return Conflict("Email is already in use");
                }

                // Ensure the current user has permissions to create an admin
                var existingAdmins = _userManager.Users.Where(u => u.Role == "Admin").ToList();
                if ((existingAdmins.Any() && model.Role != "Admin") && HttpContext.Session.GetString("UserRole") != "Admin")
                {
                    _logger.LogWarning("RegisterAdmin - Unauthorized attempt to create an admin");
                    return Unauthorized("You do not have permission to perform this action");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("RegisterAdmin - Error during validation: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during validation");
            }

            var user = new AppUser();
            user.MapFrom(model);
            user.Joined = DateTime.UtcNow;

            // Create the user in the database
            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                _logger.LogError("RegisterAdmin - Failed to create user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return BadRequest("Failed to create admin user");
            }

            // Assign the role to the user
            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("RegisterAdmin - Failed to assign role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                // Rollback user creation if role assignment fails
                await _userManager.DeleteAsync(user);
                return BadRequest("Failed to assign role to the user");
            }

            // Send the temporary password via email
            var emailResult = await _emailService.SendNewAdminEmailAsync(
                $"{user.FirstName} {user.LastName}",
                model.Password,
                user.Role,
                user.Email);

            if (!emailResult.IsSuccessful)
            {
                _logger.LogError("RegisterAdmin - Failed to send temporary password email to {Email}", user.Email);
                return BadRequest("Failed to send temporary password email");
            }

            _logger.LogInformation("RegisterAdmin - Admin user {Email} created successfully", user.Email);
            return Ok("Admin user created successfully");
        }

        [HttpPost("request-account-deletion")]
        public async Task<IActionResult> RequestAccountDeletion()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Failed to update profile - user not logged in.");
                return Unauthorized("You are not logged in.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogError("Failed to create account deletion request - user not found");
                return NotFound("User not found");
            }

            if (user.LockoutEnd != null)
            {
                return BadRequest("You already have a pending account deletion request");
            }

            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100); // Блокування акаунта як запит на видалення

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to save account deletion request for user {UserName}", user.UserName);
                return BadRequest("Failed to save account deletion request");
            }

            _logger.LogInformation("Account deletion request created successfully for user {UserName}", user.UserName);
            return Ok("Account deletion request submitted successfully");
        }

        [HttpGet("account-deletion-requests")]
        public async Task<IActionResult> GetAccountDeletionRequests()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var currentUser = await _userManager.FindByIdAsync(userId);

            if (currentUser == null)
            {
                _logger.LogWarning("Access denied - user not found.");
                return Unauthorized("User not authenticated.");
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", currentUser.UserName);
                return Forbid("You do not have permission to access this resource.");
            }

            var usersWithDeletionRequests = _userManager.Users
                .Where(u => u.LockoutEnd != null)
                .Select(u => new
                {
                    Nickname = u.UserName,
                    Email = u.Email,
                    JoinedDate = u.Joined,
                    LockoutEnd = u.LockoutEnd
                })
                .ToList();

            if (!usersWithDeletionRequests.Any())
            {
                return Ok("No account deletion requests found");
            }

            return Ok(usersWithDeletionRequests);
        }


        [HttpDelete("approve-account-deletion/{nickname}")]
        public async Task<IActionResult> ApproveAccountDeletion(string nickname)
        {
            var adminId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var adminUser = await _userManager.FindByIdAsync(adminId);
            if (adminUser == null || (adminUser.Role != "Admin" && adminUser.Role != "AdminDB"))
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", adminUser?.UserName);
                return Forbid("You do not have permission to perform this action.");
            }

            var user = await _userManager.FindByNameAsync(nickname);

            if (user == null || user.LockoutEnd == null)
            {
                _logger.LogWarning("Account deletion request not found for user {UserName}.", nickname);
                return NotFound("Account deletion request not found.");
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                _logger.LogError("Failed to delete user {UserName}", nickname);
                return BadRequest("Failed to delete user.");
            }

            _logger.LogInformation("User {UserName} deleted successfully", nickname);
            return Ok("Account deletion approved and user deleted successfully.");
        }

        [HttpPut("reject-account-deletion/{nickname}")]
        public async Task<IActionResult> RejectAccountDeletion(string nickname)
        {
            var adminId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var adminUser = await _userManager.FindByIdAsync(adminId);
            if (adminUser == null || (adminUser.Role != "Admin" && adminUser.Role != "AdminDB"))
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", adminUser?.UserName);
                return Forbid("You do not have permission to perform this action.");
            }

            var user = await _userManager.FindByNameAsync(nickname);

            if (user == null || user.LockoutEnd == null)
            {
                _logger.LogWarning("Account deletion request not found for user {UserName}.", nickname);
                return NotFound("Account deletion request not found.");
            }

            user.LockoutEnd = null; // Скасування запиту

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to reject account deletion for user {UserName}", nickname);
                return BadRequest("Failed to reject account deletion.");
            }

            _logger.LogInformation("Account deletion request rejected for user {UserName}.", nickname);
            return Ok("Account deletion request rejected successfully.");
        }


        [HttpGet("admin/users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var currentUser = await _userManager.FindByIdAsync(userId);

            if (currentUser == null)
            {
                _logger.LogWarning("Access denied - user not found.");
                return Unauthorized("User not authenticated.");
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", currentUser.UserName);
                return Forbid("You do not have permission to access this resource.");
            }

            var users = await _userManager.Users
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.Joined,
                    u.Bio,
                    u.Role,
                    u.PendingRoleChange,
                    u.DateOfBirth,
                    u.Sex,
                    u.Email,
                    u.UserName,
                    u.LockoutEnd
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPut("block/{nickname}")]
        public async Task<IActionResult> BlockUser(string nickname)
        {
            if (string.IsNullOrEmpty(nickname))
            {
                _logger.LogError("Failed to block user - nickname is null or empty.");
                return BadRequest("UserName is required.");
            }

            var adminId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var adminUser = await _userManager.FindByIdAsync(adminId);
            if (adminUser == null || (adminUser.Role != "Admin" && adminUser.Role != "AdminDB"))
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", adminUser?.UserName);
                return Forbid("You do not have permission to perform this action.");
            }

            var user = await _userManager.FindByNameAsync(nickname);

            if (user == null)
            {
                _logger.LogError("Failed to block user - user with nickname {UserName} not found.", nickname);
                return NotFound($"User with nickname '{nickname}' not found.");
            }

            if (user.Role == "Admin" || user.Role == "AdminDB")
            {
                _logger.LogWarning("Attempt to block admin user with nickname {UserName}.", nickname);
                return BadRequest("Admins cannot be blocked.");
            }

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to block user with nickname {UserName}.", nickname);
                return BadRequest("Failed to block user.");
            }

            _logger.LogInformation("User with nickname {UserName} successfully blocked.", nickname);
            return Ok($"User '{nickname}' has been blocked.");
        }

        [HttpDelete("admin/delete/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var adminId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Access denied - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var adminUser = await _userManager.FindByIdAsync(adminId);
            if (adminUser == null || (adminUser.Role != "Admin" && adminUser.Role != "AdminDB"))
            {
                _logger.LogWarning("Access denied - insufficient permissions for user {UserName}.", adminUser?.UserName);
                return Forbid("You do not have permission to perform this action.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogError("Failed to delete user - user with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            if (user.Role == "Admin" || user.Role == "AdminDB")
            {
                _logger.LogWarning("Attempt to delete admin user with ID {UserId}.", userId);
                return BadRequest("Admins cannot be deleted.");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to delete user with ID {UserId}.", userId);
                return BadRequest(result.Errors);
            }

            _logger.LogInformation("User with ID {UserId} deleted successfully.", userId);
            return Ok("User deleted successfully.");
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Очищення сесії
            _logger.LogInformation("User logged out successfully.");
            return Ok("Logged out successfully.");
        }

        [HttpPut("admin/update/role/{userId}")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateRoleModel model)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(model.NewRole))
            {
                _logger.LogError("Invalid input data for updating user role.");
                return BadRequest("User ID and new role are required.");
            }

            var adminId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Unauthorized access attempt to update user role.");
                return Unauthorized("You must be logged in as an admin.");
            }

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || (admin.Role != "Admin" && admin.Role != "AdminDB"))
            {
                _logger.LogWarning("User {UserId} is not authorized to update roles.", adminId);
                return Forbid();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("User with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            user.Role = model.NewRole;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("Failed to update role for user with ID {UserId}.", userId);
                return BadRequest("Failed to update user role.");
            }

            _logger.LogInformation("User with ID {UserId} role updated to {NewRole} by admin {AdminName}.", userId, model.NewRole, admin.UserName);
            return Ok(new { message = "User role updated successfully." });
        }

    }
}

/*
        [HttpPost("change-email")]
        [Authorize]
        public async Task<IActionResult> ChangeEmail([FromBody] RequestEmailChangeModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.NewEmail))
            {
                _logger.LogError("ChangeEmail - new email is null or empty.");
                return BadRequest("New email is required.");
            }

            // Отримання ID користувача з сесії
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("ChangeEmail - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("ChangeEmail - user with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existingUser != null)
            {
                _logger.LogWarning("ChangeEmail - email {Email} is already in use.", model.NewEmail);
                return BadRequest("This email is already in use.");
            }

            user.Email = model.NewEmail;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                _logger.LogError("ChangeEmail - failed to update email for user {UserName}.", user.UserName);
                foreach (var error in result.Errors)
                {
                    _logger.LogError("Error: {Error}", error.Description);
                }
                return BadRequest("Failed to update email.");
            }

            _logger.LogInformation("Email changed successfully for user {UserName}.", user.UserName);
            return Ok("Email changed successfully.");
        }

        [HttpPost("request-role-change")]
        public async Task<IActionResult> RequestRoleChange([FromBody] string requestedRole)
        {
            // Отримання ID користувача з сесії
            var userId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Failed to create role change request - user ID not found in session.");
                return Unauthorized("User not authenticated.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogError("Failed to create role change request - user not found");
                return NotFound("User not found");
            }

            if (!new[] { "Admin", "Shop" }.Contains(requestedRole, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid role requested");
            }

            if (!string.IsNullOrEmpty(user.PendingRoleChange))
            {
                _logger.LogWarning("Role change request already exists for user {UserName}", user.UserName);
                return BadRequest("You already have a pending role change request");
            }

            user.PendingRoleChange = requestedRole;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to save role change request for user {UserName}", user.UserName);
                return BadRequest("Failed to save role change request");
            }

            _logger.LogInformation("Role change request created successfully for user {UserName}", user.UserName);
            return Ok("Role change request submitted successfully");
        }

        [HttpGet("role-change-requests")]
        public async Task<IActionResult> GetRoleChangeRequests()
        {
            var userRole = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(userRole) || !string.Equals(userRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Access denied - user is not an admin.");
                return Unauthorized("You do not have permission to perform this action.");
            }

            var usersWithPendingRequests = _userManager.Users
                .Where(u => !string.IsNullOrEmpty(u.PendingRoleChange))
                .ToList();

            if (!usersWithPendingRequests.Any())
            {
                return Ok("No role change requests found");
            }

            return Ok(usersWithPendingRequests.Select(u => new
            {
                Nickname = u.UserName,
                CurrentRole = u.Role,
                RequestedRole = u.PendingRoleChange,
                Email = u.Email
            }));
        }

        [HttpPut("approve-role-change/{nickname}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveRoleChange(string nickname)
        {
            var user = await _userManager.FindByNameAsync(nickname);

            if (user == null || string.IsNullOrEmpty(user.PendingRoleChange))
            {
                _logger.LogError("Role change request not found for user {UserName}", nickname);
                return NotFound("Role change request not found");
            }

            user.Role = user.PendingRoleChange;
            user.PendingRoleChange = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to approve role change for user {UserName}", nickname);
                return BadRequest("Failed to approve role change");
            }

            _logger.LogInformation("Role change request approved for user {UserName}", nickname);
            return Ok("Role change request approved successfully");
        }

        [HttpPut("reject-role-change/{nickname}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectRoleChange(string nickname)
        {
            var user = await _userManager.FindByNameAsync(nickname);

            if (user == null || string.IsNullOrEmpty(user.PendingRoleChange))
            {
                _logger.LogError("Role change request not found for user {UserName}", nickname);
                return NotFound("Role change request not found");
            }

            user.PendingRoleChange = null;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to reject role change for user {UserName}", nickname);
                return BadRequest("Failed to reject role change");
            }

            _logger.LogInformation("Role change request rejected for user {UserName}", nickname);
            return Ok("Role change request rejected successfully");
        }*/
