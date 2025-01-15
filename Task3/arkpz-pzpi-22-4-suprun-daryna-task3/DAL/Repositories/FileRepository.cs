using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DAL.Repositories
{
    public class FileRepository : IFileRepository
    {
        private readonly ILogger<FileRepository> _logger;

        public FileRepository(ILogger<FileRepository> logger)
        {
            _logger = logger;
        }

        public async Task SaveFileAsync(IFormFile file, string folderPath, string fileName)
        {
            try
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folderPath);
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File {fileName} saved successfully in {folderPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save file {fileName}: {ex.Message}");
                throw;
            }
        }
    }
}
