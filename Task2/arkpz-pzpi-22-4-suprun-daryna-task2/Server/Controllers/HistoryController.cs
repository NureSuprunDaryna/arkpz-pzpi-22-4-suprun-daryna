using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.ViewModels.History;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistoryController : ControllerBase
    {
        private readonly ILogger<HistoryController> _logger;
        private readonly IHistoryService _historyService;
        private readonly IElixirService _elixirService;
        private readonly UserManager<AppUser> _userManager;

        public HistoryController(ILogger<HistoryController> logger, IHistoryService historyService, IElixirService elixirService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _historyService = historyService;
            _elixirService = elixirService;
            _userManager = userManager;
        }

        // Отримання історії для поточного користувача
        [HttpGet("user/history")]
        public async Task<IActionResult> GetUserHistory()
        {
            _logger.LogInformation("Fetching history for the current user.");

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Failed to fetch history - user is not authenticated.");
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("Failed to fetch history - user with ID {UserId} not found.", userId);
                return NotFound(new { message = "User not found." });
            }

            var elixirs = await _elixirService.GetElixirsByAuthor(userId);
            if (!elixirs.IsSuccessful || elixirs.Data == null || !elixirs.Data.Any())
            {
                _logger.LogWarning("No elixirs found for user {UserId}.", userId);
                return NotFound(new { message = "No elixirs found for the user." });
            }

            var histories = new List<History>();
            foreach (var elixir in elixirs.Data)
            {
                var elixirHistories = await _historyService.GetHistoriesByElixirId(elixir.Id);
                if (elixirHistories.IsSuccessful && elixirHistories.Data != null)
                {
                    histories.AddRange(elixirHistories.Data);
                }
            }

            if (!histories.Any())
            {
                _logger.LogWarning("No history records found for user {UserId}.", userId);
                return NotFound(new { message = "No history records found." });
            }

            var response = histories.Select(h =>
            {
                var viewModel = new HistoryModel();
                viewModel.MapFrom(h);
                return viewModel;
            }).ToList();

            _logger.LogInformation("Successfully fetched history records for the current user.");
            return Ok(new { message = "History retrieved successfully.", data = response });
        }


        // Видалення конкретного запису з історії
        [HttpDelete("delete/{historyId}")]
        public async Task<IActionResult> DeleteHistoryRecord(int historyId)
        {
            if (historyId <= 0)
            {
                _logger.LogError("Invalid history ID: {HistoryId}.", historyId);
                return BadRequest(new { message = "Invalid history ID." });
            }

            _logger.LogInformation("Attempting to delete history record with ID {HistoryId}.", historyId);

            var result = await _historyService.DeleteHistoryRecord(historyId);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to delete history record with ID {HistoryId}.", historyId);
                return BadRequest(new { message = "Failed to delete history record." });
            }

            _logger.LogInformation("History record with ID {HistoryId} deleted successfully.", historyId);
            return Ok(new { message = "History record deleted successfully." });
        }

        // Видалення всієї історії користувача
        [HttpDelete("delete/all/{userNickname}")]
        public async Task<IActionResult> DeleteAllUserHistory(string userNickname)
        {
            if (string.IsNullOrWhiteSpace(userNickname))
            {
                _logger.LogError("Invalid user nickname.");
                return BadRequest(new { message = "User nickname is required." });
            }

            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to delete history.");
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
                if (!string.Equals(currentUser.UserName, userNickname, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {UserName} attempted to delete another user's history {UserNickname}.", currentUser.UserName, userNickname);
                    return Forbid();
                }
            }

            _logger.LogInformation("User {UserName} is attempting to delete all history for user {UserNickname}.", currentUser.UserName, userNickname);

            var targetUser = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == userNickname);
            if (targetUser == null)
            {
                _logger.LogWarning("User with nickname {UserNickname} not found.", userNickname);
                return NotFound(new { message = "User not found." });
            }

            var elixirs = await _elixirService.GetElixirsByAuthor(targetUser.Id);
            if (!elixirs.IsSuccessful || elixirs.Data == null || !elixirs.Data.Any())
            {
                _logger.LogWarning("No elixirs found for user {UserNickname}.", userNickname);
                return NotFound(new { message = "No history records to delete for the user." });
            }

            var historyDeletionResults = new List<bool>();
            foreach (var elixir in elixirs.Data)
            {
                var deletionResult = await _historyService.DeleteHistoriesByElixirId(elixir.Id);
                historyDeletionResults.Add(deletionResult.IsSuccessful);
            }

            if (historyDeletionResults.All(r => r))
            {
                _logger.LogInformation("All history records for user {UserNickname} deleted successfully by {UserName}.", userNickname, currentUser.UserName);
                return Ok(new { message = "All user history deleted successfully." });
            }
            else
            {
                _logger.LogError("Failed to delete some history records for user {UserNickname}.", userNickname);
                return BadRequest(new { message = "Failed to delete some user history records." });
            }
        }

        [HttpGet("all-users-history")]
        public async Task<IActionResult> GetAllUsersHistory()
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to fetch all users' history.");
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

            _logger.LogInformation("Admin {AdminName} is fetching history for all users.", currentUser.UserName);

            try
            {
                var allHistories = await _historyService.GetAllUsersHistory();

                if (!allHistories.IsSuccessful || allHistories.Data == null || !allHistories.Data.Any())
                {
                    _logger.LogWarning("No history records found in the database.");
                    return NotFound(new { message = "No history records found." });
                }

                var response = allHistories.Data.Select(h =>
                {
                    var viewModel = new HistoryModel();
                    viewModel.MapFrom(h);
                    return viewModel;
                }).ToList();

                _logger.LogInformation("Successfully retrieved history records for all users.");
                return Ok(new { message = "History records retrieved successfully.", data = response });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while fetching all users' history. Error: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching the history records.", error = ex.Message });
            }
        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterUserHistory(
            string userNickname,
            DateTime? date,
            string? elixirName,
            [FromQuery] List<string>? noteNames,
            [FromQuery] List<string>? elixirNames)
        {
            var currentUserId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(currentUserId))
            {
                _logger.LogWarning("Unauthorized access attempt to filter history.");
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
                if (!string.Equals(currentUser.UserName, userNickname, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {UserName} tried to access another user's history.", currentUser.UserName);
                    return Forbid();
                }
            }

            _logger.LogInformation(
                "User {UserName} is filtering history for user {UserNickname}.",
                currentUser.UserName, userNickname);

            var result = await _historyService.GetUserHistory(userNickname, date, elixirName, noteNames, elixirNames);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogWarning("No matching history records found for user {UserNickname}.", userNickname);
                return NotFound(new { message = "No matching history records found." });
            }

            var response = result.Data.Select(h =>
            {
                var viewModel = new HistoryModel();
                viewModel.MapFrom(h);
                return viewModel;
            }).ToList();

            _logger.LogInformation("Successfully filtered history for user {UserNickname}.", userNickname);
            return Ok(new { message = "History filtered successfully.", data = response });
        }


        // Генерація звіту (тільки для адміністраторів)
        [HttpPost("report/{userNickname}")]
        public async Task<IActionResult> GenerateReport(string userNickname, [FromBody] ReportRequestModel request)
        {
            var adminRole = HttpContext.Session.GetString("UserRole");
            var adminName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(adminRole) || string.IsNullOrEmpty(adminName))
            {
                _logger.LogWarning("Unauthorized access attempt to generate report.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            if (adminRole != "Admin" && adminRole != "AdminDB")
            {
                _logger.LogWarning("Access denied for user {UserName} - insufficient privileges.", adminName);
                return Forbid();
            }

            if (request == null)
            {
                _logger.LogError("Invalid report request for user {UserNickname}.", userNickname);
                return BadRequest(new { message = "Report request data is required." });
            }

            _logger.LogInformation("Admin {AdminName} is generating report for user {UserNickname}.", adminName, userNickname);

            var reportRequest = new ReportRequest();
            reportRequest.MapFrom(request);

            var result = await _historyService.GenerateReport(userNickname, reportRequest);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogError("Failed to generate report for user {UserNickname}.", userNickname);
                return BadRequest(new { message = "Failed to generate report." });
            }

            _logger.LogInformation("Report generated successfully for user {UserNickname}.", userNickname);
            return Ok(new { message = "Report generated successfully.", report = result.Data });
        }

        // Генерація PDF звіту (тільки для адміністраторів)
        [HttpPost("report/pdf/{userNickname}")]
        public async Task<IActionResult> GenerateReportPdf(string userNickname, [FromBody] ReportRequestModel request)
        {
            var adminRole = HttpContext.Session.GetString("UserRole");
            var adminName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(adminRole) || string.IsNullOrEmpty(adminName))
            {
                _logger.LogWarning("Unauthorized access attempt to generate PDF report.");
                return Unauthorized(new { message = "You are not authorized to access this resource." });
            }

            if (adminRole != "Admin" && adminRole != "AdminDB")
            {
                _logger.LogWarning("Access denied for user {UserName} - insufficient privileges.", adminName);
                return Forbid();
            }

            if (request == null)
            {
                _logger.LogError("Invalid report request for user {UserNickname}.", userNickname);
                return BadRequest(new { message = "Report request data is required." });
            }

            _logger.LogInformation("Admin {AdminName} is generating PDF report for user {UserNickname}.", adminName, userNickname);

            var reportRequest = new ReportRequest();
            reportRequest.MapFrom(request);

            var result = await _historyService.GenerateReportPdf(userNickname, reportRequest);

            if (!result.IsSuccessful || result.Data == null)
            {
                _logger.LogError("Failed to generate PDF report for user {UserNickname}.", userNickname);
                return NotFound(new { message = "Failed to generate PDF report." });
            }

            _logger.LogInformation("PDF report generated successfully for user {UserNickname}.", userNickname);
            return File(result.Data, "application/pdf", $"{userNickname}_Report.pdf");
        }

    }
}
