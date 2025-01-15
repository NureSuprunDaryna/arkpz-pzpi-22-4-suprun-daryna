using BLL.Interfaces;
using Core.Models;
using DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class DeviceService : GenericService<Device>, IDeviceService
    {
        private readonly ILogger<DeviceService> _logger;
        private readonly IRepository<Device> _deviceRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeviceService(UnitOfWork unitOfWork, ILogger<DeviceService> logger,
            UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, unitOfWork.DeviceRepository)
        {
            _logger = logger;
            _deviceRepository = unitOfWork.DeviceRepository;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<Device>> AddDevice(Device device)
        {
            _logger.LogInformation("Attempting to add a new device with name {DeviceName}.", device.Name);

            try
            {
                var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Failed to add device - user ID not found in session.");
                    return new Result<Device>(false);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("Failed to add device - user with ID {UserId} not found.", userId);
                    return new Result<Device>(false);
                }

                var existingDevice = await _deviceRepository.GetAsync(d => d.Name == device.Name);
                if (existingDevice.Any())
                {
                    _logger.LogWarning("Device with name {DeviceName} already exists.", device.Name);
                    return new Result<Device>(false);
                }

                device.DateOfRegistration = DateTime.UtcNow;

                await _deviceRepository.AddAsync(device);
                await _unitOfWork.Save();

                _logger.LogInformation("Device with name {DeviceName} successfully added.", device.Name);
                return new Result<Device>(true, device);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while adding device: {Error}", ex.Message);
                return new Result<Device>(false);
            }
        }

        public async Task<Result<string>> GetDeviceStatus(int deviceId)
        {
            _logger.LogInformation("Fetching status for device with ID {DeviceId}.", deviceId);

            try
            {
                var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Failed to get device status - user ID not found in session.");
                    return new Result<string>(false);
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogError("Failed to fetch device status - user not found.");
                    return new Result<string>(false);
                }

                var device = await _deviceRepository.GetByIdAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found.", deviceId);
                    return new Result<string>(false, "Device not found.");
                }

                _logger.LogInformation("Successfully fetched status for device with ID {DeviceId}. Status: {Status}.", deviceId, device.Status);
                return new Result<string>(true, device.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while fetching status for device with ID {DeviceId}: {Error}", deviceId, ex.Message);
                return new Result<string>(false);
            }
        }

        public async Task<Result> SendCommandToDevice(int deviceId, string command)
        {
            _logger.LogInformation("Sending control command to device with ID {DeviceId}. Command: {Command}", deviceId, command);

            try
            {
                var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Failed to send command to device - user ID not found in session.");
                    return new Result<string>(false);
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    _logger.LogError("Failed to control device - user not found.");
                    return new Result(false);
                }

                var device = await _deviceRepository.GetByIdAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found.", deviceId);
                    return new Result(false);
                }

                var controlResult = await SendCommandToDevice(device, command);

                if (!controlResult.IsSuccessful)
                {
                    _logger.LogError("Failed to send command to device with ID {DeviceId}.", deviceId);
                    return new Result(false);
                }

                _logger.LogInformation("Successfully sent command to device with ID {DeviceId}.", deviceId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while controlling device with ID {DeviceId}: {Error}", deviceId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> SendCommandToDevice(Device device, string command)
        {
            // Логіка для відправки команди на пристрій
            // Наприклад, через HTTP-запит до пристрою або WebSocket
            _logger.LogInformation("Simulating sending command to device {DeviceId}. Command: {Command}", device.Id, command);
            await Task.Delay(500); // Симуляція затримки
            return new Result(true); // Припускаємо, що команда успішно відправлена
        }

        public async Task<Result<List<Device>>> MonitorAllDevices() //admin
        {
            _logger.LogInformation("Admin is fetching the status of all devices.");

            try
            {
                var devices = await _deviceRepository.GetAsync();

                if (devices == null || !devices.Any())
                {
                    _logger.LogWarning("No devices found in the system.");
                    return new Result<List<Device>>(false);
                }

                _logger.LogInformation("Successfully retrieved {Count} devices.", devices.Count);
                return new Result<List<Device>>(true, devices.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while fetching all devices: {Message}", ex.Message);
                return new Result<List<Device>>(false);
            }
        }

        public async Task<Result> ResolveDeviceError(int deviceId, string errorDescription) //admin
        {
            _logger.LogInformation("Admin is resolving the error for device with ID {DeviceId}. Description: {ErrorDescription}", deviceId, errorDescription);

            try
            {
                var device = await _deviceRepository.GetByIdAsync(deviceId);

                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found.", deviceId);
                    return new Result(false);
                }

                if (device.Status != "error")
                {
                    _logger.LogInformation("Device with ID {DeviceId} is not in an error state.", deviceId);
                    return new Result(false);
                }

                _logger.LogInformation("Error description for device {DeviceId}: {ErrorDescription}", deviceId, errorDescription);

                device.Status = "active";

                await _unitOfWork.Save();

                _logger.LogInformation("Successfully resolved the error for device with ID {DeviceId}.", deviceId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while resolving the error for device with ID {DeviceId}: {ErrorMessage}", deviceId, ex.Message);
                return new Result(false);
            }
        }

    }
}
