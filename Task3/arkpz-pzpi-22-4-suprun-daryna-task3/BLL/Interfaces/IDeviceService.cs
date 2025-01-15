using Core.Models;

namespace BLL.Interfaces
{
    public interface IDeviceService : IGenericService<Device>
    {
        // Функціональність для користувачів
        Task<Result<Device>> AddDevice(Device device); // Додавання нового пристрою
        Task<Result<string>> GetDeviceStatus(int deviceId); // Перегляд статусу пристрою
        Task<Result> SendCommandToDevice(int deviceId, string command); // Змішування аромату на пристрої //Iot-device bll

        // Функціональність для адміністраторів
        Task<Result<List<Device>>> MonitorAllDevices(); // Моніторинг роботи пристроїв
        Task<Result> ResolveDeviceError(int deviceId, string errorDescription); // Виявлення та усунення помилок у роботі //Iot-device bll

    }
}
