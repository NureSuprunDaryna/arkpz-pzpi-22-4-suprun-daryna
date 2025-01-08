using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Server.ViewModels.Device;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceController : ControllerBase
    {
        private readonly ILogger<DeviceController> _logger;
        private readonly IDeviceService _deviceService;
        private readonly UserManager<AppUser> _userManager;

        public DeviceController(ILogger<DeviceController> logger, IDeviceService deviceService, UserManager<AppUser> userManager)
        {
            _logger = logger;
            _deviceService = deviceService;
            _userManager = userManager;
        }

        // Додавання нового пристрою
        [HttpPost("create")]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.LogError("Failed to create device - input model is null or name is empty.");
                return BadRequest("Device name is required.");
            }

            _logger.LogInformation("Attempting to create a new device with name: {DeviceName}", model.Name);

            var device = new Device();
            device.MapFrom(model);

            var result = await _deviceService.AddDevice(device);

            if (result.IsSuccessful)
            {
                var responseModel = new CreateDeviceResponseModel();
                responseModel.MapFrom(result.Data);

                _logger.LogInformation("Device created successfully with ID {DeviceId}.", result.Data.Id);
                return Ok(responseModel);
            }
            else
            {
                _logger.LogError("Failed to create device with name: {DeviceName}.", model.Name);
                return BadRequest("Failed to create device.");
            }
        }

        // Перегляд статусу пристрою
        [HttpGet("{deviceId}/status")]
        public async Task<IActionResult> GetDeviceStatus(int deviceId)
        {
            if (deviceId <= 0)
            {
                _logger.LogError("Invalid device ID: {DeviceId}.", deviceId);
                return BadRequest("Invalid device ID.");
            }

            _logger.LogInformation("Fetching status for device with ID {DeviceId}.", deviceId);

            var result = await _deviceService.GetDeviceStatus(deviceId);

            if (!result.IsSuccessful)
            {
                _logger.LogWarning("Failed to fetch status for device with ID {DeviceId}.", deviceId);
                return NotFound("Device not found or unauthorized access.");
            }

            var responseModel = new GetDeviceStatusModel
            {
                Id = deviceId,
                Status = result.Data
            };

            _logger.LogInformation("Successfully fetched status for device with ID {DeviceId}.", deviceId);
            return Ok(responseModel);
        }


        // Відправлення команди на пристрій
        [HttpPost("{deviceId}/command")]
        public async Task<IActionResult> SendDeviceCommand(int deviceId, [FromBody] string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                _logger.LogError("Failed to send command - command is null or empty.");
                return BadRequest(new { message = "Command is required." });
            }

            _logger.LogInformation("Sending command to device with ID {DeviceId}. Command: {Command}", deviceId, command);

            var result = await _deviceService.SendCommandToDevice(deviceId, command);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to send command to device with ID {DeviceId}.", deviceId);
                return BadRequest(new { message = "Failed to send command." });
            }

            _logger.LogInformation("Command sent successfully to device with ID {DeviceId}.", deviceId);
            return Ok(new { message = "Command sent successfully." });
        }

        // Моніторинг усіх пристроїв (тільки адміністратор)
        [HttpGet("monitor")]
        public async Task<IActionResult> MonitorDevices()
        {
            var adminId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Admin not authenticated.");
                return Unauthorized("Admin not authenticated.");
            }

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || admin.Role != "Admin" || admin.Role != "AdminDB")
            {
                _logger.LogWarning("Unauthorized attempt to monitor devices.");
                return Forbid("You do not have sufficient permissions.");
            }

            _logger.LogInformation("Admin {AdminName} is fetching all devices for monitoring.", admin.UserName);

            var result = await _deviceService.MonitorAllDevices();

            if (!result.IsSuccessful || result.Data == null || !result.Data.Any())
            {
                _logger.LogWarning("No devices found for monitoring.");
                return NotFound(new { message = "No devices available." });
            }

            _logger.LogInformation("Successfully retrieved {DeviceCount} devices for monitoring.", result.Data.Count);
            return Ok(new { message = "Devices retrieved successfully.", devices = result.Data });
        }


        // Виявлення та усунення помилок пристрою (тільки адміністратор)
        [HttpPost("{deviceId}/resolve-error")]
        public async Task<IActionResult> ResolveDeviceError(int deviceId, [FromBody] string errorDescription)
        {
            var adminId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(adminId))
            {
                _logger.LogWarning("Admin not authenticated.");
                return Unauthorized(new { message = "Admin not authenticated." });
            }

            var admin = await _userManager.FindByIdAsync(adminId);
            if (admin == null || admin.Role != "Admin" || admin.Role != "AdminDB")
            {
                _logger.LogWarning("Unauthorized attempt to resolve device error.");
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(errorDescription))
            {
                _logger.LogError("Failed to resolve device error - error description is null or empty.");
                return BadRequest(new { message = "Error description is required." });
            }

            _logger.LogInformation("Admin {AdminName} is resolving error for device with ID {DeviceId}.", admin.UserName, deviceId);

            var result = await _deviceService.ResolveDeviceError(deviceId, errorDescription);

            if (!result.IsSuccessful)
            {
                _logger.LogError("Failed to resolve error for device with ID {DeviceId}.", deviceId);
                return BadRequest(new { message = "Failed to resolve error." });
            }

            _logger.LogInformation("Error resolved successfully for device with ID {DeviceId}.", deviceId);
            return Ok(new { message = "Device error resolved successfully." });
        }
    }
}
