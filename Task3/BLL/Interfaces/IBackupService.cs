using Core.Models;

namespace BLL.Interfaces
{
    public interface IBackupService
    {
        Task<Result> CreateBackupAsync();
        Task<Result> RestoreBackupAsync(string backupFilePath);
    }
}

