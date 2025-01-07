using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.ViewModels.Elixir;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ElixirController : ControllerBase
    {
        private readonly ILogger<ElixirController> _logger;
        private readonly IElixirService _elixirService;
        private readonly UserManager<AppUser> _userManager;

        public ElixirController(ILogger<ElixirController> logger, IElixirService elixirService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _elixirService = elixirService;
            _userManager = userManager;
        }

        // Створення нового еліксиру
        [HttpPost("create")]
        public async Task<IActionResult> CreateElixir([FromBody] CreateElixirModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogError("Failed to create elixir - invalid input data.");
                return BadRequest("Elixir name is required.");
            }

            _logger.LogInformation("Attempting to create a new elixir with name: {ElixirName}", model.Name);

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Failed to create elixir - user is not authenticated.");
                return Unauthorized("User is not authenticated.");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("Failed to create elixir - user with ID {UserId} not found.", userId);
                return NotFound("User not found.");
            }

            var elixir = new Elixir
            {
                Name = model.Name,
                CreationDate = DateTime.UtcNow,
                Keywords = model.Keywords,
                Description = model.Description,
                AuthorId = user.Id,
                Author = user
            };

            var result = await _elixirService.CreateElixir(elixir);

            if (result.IsSuccessful)
            {
                var response = new ElixirResponseModel();
                response.MapFrom(result.Data);

                _logger.LogInformation("Elixir created successfully with ID {ElixirId}.", result.Data.Id);
                return Ok(response);
            }

            _logger.LogError("Failed to create elixir with name: {ElixirName}.", model.Name);
            return BadRequest("Failed to create elixir.");
        }


        // Редагування еліксиру
        [HttpPut("update")]
        public async Task<IActionResult> EditElixir([FromBody] UpdateElixirModel model)
        {
            if (model == null || model.Id <= 0)
            {
                _logger.LogError("Failed to update elixir - invalid input data.");
                return BadRequest("Invalid elixir data.");
            }

            var existingElixir = await _elixirService.GetById(model.Id);

            if (!existingElixir.IsSuccessful)
            {
                _logger.LogError("Elixir with ID {ElixirId} not found.", model.Id);
                return NotFound("Elixir not found.");
            }

            existingElixir.Data.MapFrom(model);

            var result = await _elixirService.EditElixir(existingElixir.Data);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to update elixir with ID {ElixirId}.", model.Id);
                return BadRequest("Failed to update elixir.");
            }

            _logger.LogInformation("Elixir with ID {ElixirId} updated successfully.", model.Id);
            return Ok(new { message = "Elixir updated successfully." });
        }

        // Видалення еліксиру
        [HttpDelete("delete/{elixirId}")]
        public async Task<IActionResult> DeleteElixir(int elixirId)
        {
            if (elixirId <= 0)
            {
                _logger.LogError("Invalid elixir ID: {ElixirId}.", elixirId);
                return BadRequest("Invalid elixir ID.");
            }

            var result = await _elixirService.DeleteElixir(elixirId);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to delete elixir with ID {ElixirId}.", elixirId);
                return BadRequest("Failed to delete elixir.");
            }

            _logger.LogInformation("Elixir with ID {ElixirId} deleted successfully.", elixirId);
            return Ok(new { message = "Elixir deleted successfully." });
        }

        // Збереження улюбленого еліксиру
        [HttpPost("favorite/{elixirId}")]
        public async Task<IActionResult> SaveFavoriteElixir(int elixirId)
        {
            if (elixirId <= 0)
            {
                _logger.LogError("Invalid elixir ID: {ElixirId}.", elixirId);
                return BadRequest("Invalid elixir ID.");
            }

            var result = await _elixirService.SaveFavoriteElixir(elixirId);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to save favorite elixir with ID {ElixirId}.", elixirId);
                return BadRequest("Failed to save favorite elixir.");
            }

            _logger.LogInformation("Elixir with ID {ElixirId} saved to favorites successfully.", elixirId);
            return Ok(new { message = "Elixir saved to favorites successfully." });
        }

        [HttpGet("author/{authorId}")]
        public async Task<IActionResult> GetElixirsByAuthor(string authorId)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to get elixirs by author.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", currentUserId);
                return Unauthorized(new { message = "User not found." });
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("Access denied for user {UserName} - insufficient privileges.", currentUser.UserName);
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(authorId))
            {
                _logger.LogError("Invalid author ID.");
                return BadRequest("Author ID is required.");
            }

            var result = await _elixirService.GetElixirsByAuthor(authorId);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to fetch elixirs by author {AuthorId}.", authorId);
                return BadRequest("Failed to fetch elixirs.");
            }

            _logger.LogInformation("Successfully fetched elixirs for author {AuthorId}.", authorId);
            return Ok(result.Data);
        }

        [HttpPut("update/composition/{elixirId}")]
        public async Task<IActionResult> UpdateElixirComposition(int elixirId, [FromBody] List<UpdateCompositionModel> updatedNotes)
        {
            if (elixirId <= 0 || updatedNotes == null || !updatedNotes.Any())
            {
                _logger.LogError("Invalid input data for updating elixir composition.");
                return BadRequest("Elixir ID and updated notes are required.");
            }

            var notes = updatedNotes.Select(n => (n.NoteId, n.NoteCategory, n.Proportion)).ToList();
            var result = await _elixirService.UpdateElixirComposition(elixirId, notes);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to update elixir composition for ID {ElixirId}.", elixirId);
                return BadRequest("Failed to update elixir composition.");
            }

            _logger.LogInformation("Successfully updated elixir composition for ID {ElixirId}.", elixirId);
            return Ok("Elixir composition updated successfully.");
        }

        [HttpGet("composition/{elixirId}")]
        public async Task<IActionResult> GetElixirComposition(int elixirId)
        {
            if (elixirId <= 0)
            {
                _logger.LogError("Invalid elixir ID provided for retrieving composition.");
                return BadRequest("Elixir ID is required and must be a positive number.");
            }

            _logger.LogInformation("Attempting to retrieve composition for elixir with ID {ElixirId}.", elixirId);

            var result = await _elixirService.GetElixirComposition(elixirId);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogError("Failed to retrieve composition for elixir with ID {ElixirId}.", elixirId);
                return NotFound("Elixir composition not found.");
            }

            _logger.LogInformation("Successfully retrieved composition for elixir with ID {ElixirId}.", elixirId);
            return Ok(result.Data);
        }



        [HttpDelete("admin/delete/{elixirId}")]
        public async Task<IActionResult> DeleteElixirByAdmin(int elixirId)
        {
            if (elixirId <= 0)
            {
                _logger.LogError("Invalid elixir ID: {ElixirId}.", elixirId);
                return BadRequest("Invalid elixir ID.");
            }

            var result = await _elixirService.DeleteElixirByAdmin(elixirId);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to delete elixir with ID {ElixirId} by admin.", elixirId);
                return BadRequest("Failed to delete elixir.");
            }

            _logger.LogInformation("Elixir with ID {ElixirId} deleted successfully by admin.", elixirId);
            return Ok(new { message = "Elixir deleted successfully by admin." });
        }

        [HttpGet("moderate")]
        public async Task<IActionResult> ModerateElixirs()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to moderate elixirs.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            if (currentUser == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", currentUserId);
                return Unauthorized(new { message = "User not found." });
            }

            if (currentUser.Role != "Admin" && currentUser.Role != "AdminDB")
            {
                _logger.LogWarning("Access denied for user {UserName} - insufficient privileges.", currentUser.UserName);
                return Forbid();
            }

            var result = await _elixirService.ModerateElixirs();

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to retrieve elixirs for moderation.");
                return BadRequest("Failed to retrieve elixirs for moderation.");
            }

            _logger.LogInformation("Successfully retrieved elixirs for moderation.");
            return Ok(result.Data);
        }


        [HttpGet("admin/statistics")]
        public async Task<IActionResult> GetSystemStatistics()
        {
            var adminId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Unauthorized access attempt to view statistics.");
                return Unauthorized("You must be logged in as an admin.");
            }

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || (admin.Role != "Admin" && admin.Role != "AdminDB"))
            {
                _logger.LogWarning("User {UserId} is not authorized to view statistics.", adminId);
                return Forbid();
            }

            var userCount = await _userManager.Users.CountAsync();
            var elixirCount = (await _elixirService.GetElixirs()).Data?.Count ?? 0;

            return Ok(new
            {
                Users = userCount,
                Elixirs = elixirCount
            });
        }

        [HttpGet("admin/elixirs")]
        public async Task<IActionResult> GetAllElixirs()
        {
            var adminId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Unauthorized access attempt to view all elixirs.");
                return Unauthorized("You must be logged in as an admin.");
            }

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || (admin.Role != "Admin" && admin.Role != "AdminDB"))
            {
                _logger.LogWarning("User {UserId} is not authorized to view all elixirs.", adminId);
                return Forbid();
            }

            var result = await _elixirService.GetElixirs();

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to fetch all elixirs.");
                return BadRequest("Failed to fetch elixirs.");
            }

            _logger.LogInformation("Successfully fetched all elixirs.");
            return Ok(result.Data);
        }

    }
}
