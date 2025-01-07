using Core.Models;

namespace BLL.Interfaces
{
    public interface IHistoryService : IGenericService<History>
    {
        // Функціональність для користувачів
        Task<Result> DeleteHistoryRecord(int historyId); // Видалення конкретного запису з історії
        Task<Result> DeleteAllUserHistory(string userNickname); // Видалення всієї історії користувача
        Task<Result<List<History>>> GetHistoriesByElixirId(int elixirId);
        Task<Result> DeleteHistoriesByElixirId(int elixirId);

        // Функціональність для адміністраторів
        Task<Result<List<History>>> GetUserHistory(string userNickname, DateTime? date, string? elixirName, List<string>? noteNames, List<string>? elixirNames); // Перегляд історії створених ароматів, фільтрація історії за датою, нотами, назвами еліксирів
        Task<Result<List<History>>> GetAllUsersHistory(); // Моніторинг активності користувачів
        Task<Result<byte[]>> GenerateReportPdf(string userNickname, ReportRequest request);
        Task<Result<Dictionary<string, object>>> GenerateReport(string userNickname, ReportRequest request);
    }
}
