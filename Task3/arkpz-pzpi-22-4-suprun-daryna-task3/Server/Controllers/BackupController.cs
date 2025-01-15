using BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BackupController : ControllerBase
    {
        private readonly IBackupService _backupService;
        private readonly ILogger<BackupController> _logger;

        public BackupController(IBackupService backupService, ILogger<BackupController> logger)
        {
            _backupService = backupService;
            _logger = logger;
        }

        private bool IsAdminDb()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role) || !string.Equals(role, "AdminDB", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Access denied. Current user role: {Role}", role ?? "null");
                return false;
            }
            return true;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBackup()
        {
            if (!IsAdminDb())
            {
                return Forbid();
            }

            try
            {
                var result = await _backupService.CreateBackupAsync();

                if (!result.IsSuccessful)
                {
                    _logger.LogError("Failed to create backup.");
                    return StatusCode(500);
                }

                _logger.LogInformation("Backup created successfully.");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while creating backup: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An internal server error occurred.");
            }
        }



        [HttpPost("restore")]
        public async Task<IActionResult> RestoreBackup([FromQuery] string backupFilePath)
        {
            if (!IsAdminDb())
            {
                return Forbid(); // Використовується стандартна схема
            }

            if (string.IsNullOrWhiteSpace(backupFilePath))
            {
                _logger.LogError("RestoreBackup - backup file path is null or empty.");
                return BadRequest("Backup file path is required.");
            }

            try
            {
                var result = await _backupService.RestoreBackupAsync(backupFilePath);

                if (!result.IsSuccessful)
                {
                    _logger.LogError("Failed to restore backup.");
                    return StatusCode(500, "Failed to restore backup.");
                }

                _logger.LogInformation("Backup restored successfully from {BackupFilePath}.", backupFilePath);
                return Ok("Backup restored successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred while restoring backup: {ErrorMessage}", ex.Message);
                return StatusCode(500, "An internal server error occurred.");
            }
        }
    }
}
