using BLL.Interfaces;
using Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class BackupService : IBackupService
    {
        private readonly string _backupDirectory;
        private readonly string _connectionString;
        private readonly ILogger<BackupService> _logger;

        public BackupService(string connectionString, ILogger<BackupService> logger, IConfiguration configuration)
        {
            _connectionString = connectionString;
            _logger = logger;
            _backupDirectory = configuration["BackupSettings:Directory"];
        }

        public async Task<Result> CreateBackupAsync()
        {
            try
            {
                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                }

                var backupFilePath = Path.Combine(_backupDirectory, $"ElixirNocturne_{DateTime.Now:yyyyMMddHHmmss}.bak");
                var backupQuery = $"BACKUP DATABASE [ElixirNocturne] TO DISK = '{backupFilePath}'";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(backupQuery, connection);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database backup created successfully at {DestinationPath}.", backupFilePath);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create database backup. Error: {ErrorMessage}", ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> RestoreBackupAsync(string backupFilePath)
        {
            try
            {
                var restoreQuery = $"RESTORE DATABASE [ElixirNocturne] FROM DISK = '{backupFilePath}' WITH REPLACE";

                using var connection = new SqlConnection(_connectionString);
                using var command = new SqlCommand(restoreQuery, connection);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database restored successfully from {BackupFilePath}.", backupFilePath);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to restore database. Error: {ErrorMessage}", ex.Message);
                return new Result(false);
            }
        }
    }
}
