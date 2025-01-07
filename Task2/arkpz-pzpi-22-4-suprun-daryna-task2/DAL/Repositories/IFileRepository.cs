using Microsoft.AspNetCore.Http;

namespace DAL.Repositories
{
    public interface IFileRepository
    {
        Task SaveFileAsync(IFormFile file, string folderPath, string fileName);
    }
}
