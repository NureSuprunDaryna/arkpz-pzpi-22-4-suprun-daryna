using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.ViewModels.Preferences;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreferencesController : ControllerBase
    {
        private readonly ILogger<PreferencesController> _logger;
        private readonly IPreferencesService _preferencesService;
        private readonly UserManager<AppUser> _userManager;

        public PreferencesController(ILogger<PreferencesController> logger, IPreferencesService preferencesService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _preferencesService = preferencesService;
            _userManager = userManager;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePreferences([FromBody] CreatePreferencesModel model)
        {
            if (model == null)
            {
                _logger.LogError("Failed to create preferences - input data is null.");
                return BadRequest(new { message = "Preferences data is null." });
            }

            var preferences = new Preferences();
            preferences.MapFrom(model);

            _logger.LogInformation("Creating preferences for user {UserId}.", model.UserId);

            var result = await _preferencesService.AddPreferences(preferences);

            if (result.IsSuccessful)
            {
                _logger.LogInformation("Preferences created successfully with ID {PreferencesId}.", result.Data.Id);
                return Ok(new { message = "Preferences created successfully.", preferencesId = result.Data.Id });
            }

            _logger.LogError("Failed to create preferences for user {UserId}.", model.UserId);
            return BadRequest(new { message = "Failed to create preferences." });
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdatePreferences([FromBody] UpdatePreferencesModel model)
        {
            if (model == null)
            {
                _logger.LogError("Failed to update preferences - input data is null.");
                return BadRequest(new { message = "Preferences data is null." });
            }

            var preferences = new Preferences();
            preferences.MapFrom(model);

            _logger.LogInformation("Updating preferences for user {UserId}.", model.UserId);

            var result = await _preferencesService.UpdatePreferences(preferences);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to update preferences for user {UserId}.", model.UserId);
                return BadRequest(new { message = "Failed to update preferences." });
            }

            _logger.LogInformation("Preferences updated successfully for user {UserId}.", model.UserId);
            return Ok(new { message = "Preferences updated successfully." });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeletePreferences(int id)
        {
            if (id <= 0)
            {
                _logger.LogError("Failed to delete preferences - invalid ID provided: {Id}.", id);
                return BadRequest(new { message = "Invalid preferences ID." });
            }

            _logger.LogInformation("Deleting preferences with ID {Id}.", id);

            var result = await _preferencesService.DeletePreferences(id);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to delete preferences with ID {Id}.", id);
                return BadRequest(new { message = "Failed to delete preferences." });
            }

            _logger.LogInformation("Preferences with ID {Id} deleted successfully.", id);
            return Ok(new { message = "Preferences deleted successfully." });
        }

        [HttpGet("user/{userId}")] // admin
        public async Task<IActionResult> GetPreferencesByUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("Failed to fetch preferences - user ID is null or empty.");
                return BadRequest(new { message = "User ID is required." });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to fetch preferences.");
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
                _logger.LogWarning("User {UserName} does not have admin privileges.", currentUser.UserName);
                return Forbid();
            }

            _logger.LogInformation("Admin {AdminName} is fetching preferences for user {UserId}.", currentUser.UserName, userId);

            var result = await _preferencesService.GetPreferencesByUser(userId);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogWarning("No preferences found for user {UserId}.", userId);
                return NotFound(new { message = "No preferences found for the user." });
            }

            var viewModel = new PreferencesViewModel();
            viewModel.MapFrom(result.Data);

            _logger.LogInformation("Preferences fetched successfully for user {UserId} by admin {AdminName}.", userId, currentUser.UserName);
            return Ok(viewModel);
        }

        /*
        [HttpGet("elixir/{elixirId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPreferencesByElixir(int elixirId)
        {
            if (elixirId <= 0)
            {
                _logger.LogError("Failed to fetch preferences - invalid elixir ID provided: {ElixirId}.", elixirId);
                return BadRequest(new { message = "Invalid elixir ID." });
            }

            _logger.LogInformation("Fetching preferences for elixir with ID {ElixirId}.", elixirId);

            var result = await _preferencesService.GetPreferencesByElixir(elixirId);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogWarning("No preferences found for elixir with ID {ElixirId}.", elixirId);
                return NotFound(new { message = "No preferences found for the elixir." });
            }

            var viewModel = new PreferencesViewModel();
            viewModel.MapFrom(result.Data);

            _logger.LogInformation("Preferences fetched successfully for elixir with ID {ElixirId}.", elixirId);
            return Ok(viewModel);
        }
        */
    }
}
