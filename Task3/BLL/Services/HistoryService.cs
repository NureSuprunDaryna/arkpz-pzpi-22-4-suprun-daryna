using BLL.Interfaces;
using Core.Models;
using DAL.Repositories;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class HistoryService : GenericService<History>, IHistoryService
    {
        private readonly ILogger<HistoryService> _logger;
        private readonly IRepository<History> _repository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IRepository<Elixir> _elixirRepository;
        private readonly IRepository<Device> _deviceRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HistoryService(UnitOfWork unitOfWork, ILogger<HistoryService> logger, UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor)
    : base(unitOfWork, unitOfWork.HistoryRepository)
        {
            _logger = logger;
            _repository = unitOfWork.HistoryRepository;
            _userManager = userManager;
            _elixirRepository = unitOfWork.ElixirRepository;
            _deviceRepository = unitOfWork.DeviceRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<List<History>>> GetHistoriesByElixirId(int elixirId)
        {
            var histories = await _repository.GetAsync(h => h.ElixirId == elixirId);
            return new Result<List<History>>(true, histories.ToList());
        }

        public async Task<Result> DeleteHistoriesByElixirId(int elixirId)
        {
            try
            {
                var histories = await _repository.GetAsync(h => h.ElixirId == elixirId);
                if (histories.Any())
                {
                    foreach (var history in histories)
                    {
                        await _repository.DeleteAsync(history);
                    }
                    await _unitOfWork.Save();
                }

                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error while deleting histories for Elixir ID {ElixirId}: {Error}", elixirId, ex.Message);
                return new Result(false);
            }
        }


        public async Task<Result<List<History>>> GetUserHistory(string userNickname, DateTime? date, string? elixirName, List<string>? noteNames, List<string>? elixirNames)
        {
            _logger.LogInformation("Filtering history for user {Id}.", userNickname);

            try
            {
                var user = await _userManager.FindByNameAsync(userNickname);
                if (user == null)
                {
                    _logger.LogWarning("User with nickname {Id} not found.", userNickname);
                    return new Result<List<History>>(false);
                }

                var query = await _repository.GetAsync(h => h.Elixir.AuthorId == userNickname);

                if (date.HasValue)
                {
                    query = query.Where(h => h.DateTime.Date == date.Value.Date).ToList();
                }

                if (!string.IsNullOrEmpty(elixirName))
                {
                    query = query.Where(h => h.Elixir.Name.Contains(elixirName, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                if (noteNames != null && noteNames.Any())
                {
                    query = query.Where(h => h.Elixir.ElixirComposition
                        .Any(ec => noteNames.Contains(ec.Note.Name, StringComparer.OrdinalIgnoreCase))).ToList();
                }

                if (elixirNames != null && elixirNames.Any())
                {
                    query = query.Where(h => elixirNames.Contains(h.Elixir.Name)).ToList();
                }

                if (!query.Any())
                {
                    _logger.LogWarning("No matching history records found for user {Id}.", userNickname);
                    return new Result<List<History>>(false);
                }

                _logger.LogInformation("Successfully filtered history for user {Id}.", userNickname);
                return new Result<List<History>>(true, query);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error filtering history for user {Id}. Error: {Message}", userNickname, ex.Message);
                return new Result<List<History>>(false);
            }
        }

        public async Task<Result> DeleteHistoryRecord(int historyId)
        {
            _logger.LogInformation("Attempting to delete history record with ID {HistoryId}.", historyId);

            try
            {
                var historyRecord = await _repository.GetByIdAsync(historyId);
                if (historyRecord == null)
                {
                    _logger.LogWarning("History record with ID {HistoryId} not found.", historyId);
                    return new Result(false);
                }

                await _repository.DeleteAsync(historyRecord);
                await _unitOfWork.Save();

                _logger.LogInformation("Successfully deleted history record with ID {HistoryId}.", historyId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while deleting history record with ID {HistoryId}. Error: {Message}", historyId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> DeleteAllUserHistory(string userId)
        {
            _logger.LogInformation("Attempting to delete all history records for user {Id}.", userId);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with nickname {Id} not found.", userId);
                    return new Result(false);
                }

                var userHistories = await _repository.GetAsync(h => h.Elixir.AuthorId == userId);

                if (!userHistories.Any())
                {
                    _logger.LogWarning("No history records found for user {Id}.", userId);
                    return new Result(false);
                }

                foreach (var history in userHistories)
                {
                    await _repository.DeleteAsync(history);
                }
                await _unitOfWork.Save();

                _logger.LogInformation("Successfully deleted all history records for user {Id}.", userId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while deleting history records for user {Id}. Error: {Message}", userId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result<List<History>>> GetAllUsersHistory()
        {
            _logger.LogInformation("Fetching all history records.");

            try
            {
                var allHistories = await _repository.GetAsync();

                if (allHistories == null || !allHistories.Any())
                {
                    _logger.LogWarning("No history records found in the database.");
                    return new Result<List<History>>(false);
                }

                _logger.LogInformation("Successfully retrieved all history records.");
                return new Result<List<History>>(true, allHistories);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while fetching all histories. Error: {Message}", ex.Message);
                return new Result<List<History>>(false);
            }
        }

        public async Task<Result<Dictionary<string, object>>> GenerateReport(string userId, ReportRequest request)
        {
            _logger.LogInformation("Generating report for user {Id}", userId);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with id {Id} not found", userId);
                    return new Result<Dictionary<string, object>>(false);
                }

                var report = new Dictionary<string, object>();

                if (request.IncludeGeneralStatistics)
                {
                    var generalStats = await GetGeneralStatistics(user, request.StartDate, request.EndDate, request.TopNoteCount);
                    report["GeneralStatistics"] = generalStats;
                }

                if (request.IncludeActionHistory)
                {
                    var actionHistory = await GetActionHistory(user, request.StartDate, request.EndDate);
                    report["ActionHistory"] = actionHistory;
                }

                if (request.IncludeInteractiveInfo)
                {
                    var interactiveInfo = await GetInteractiveInfo(user, request.StartDate, request.EndDate);
                    report["InteractiveInfo"] = interactiveInfo;
                }

                if (request.IncludeRatings)
                {
                    var ratings = await GetRatings(user, request.StartDate, request.EndDate);
                    report["Ratings"] = ratings;
                }

                _logger.LogInformation("Report successfully generated for user {Nickname}", userId);
                return new Result<Dictionary<string, object>>(true, report);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error generating report for user {Id}. Error: {Message}", userId, ex.Message);
                return new Result<Dictionary<string, object>>(false);
            }
        }

        private async Task<object> GetGeneralStatistics(AppUser user, DateTime? startDate, DateTime? endDate, int topNoteCount)
        {
            var elixirs = user.Elixirs
                .Where(e => (!startDate.HasValue || e.CreationDate >= startDate)
                         && (!endDate.HasValue || e.CreationDate <= endDate))
                .ToList();

            var totalElixirs = elixirs.Count;
            var totalNotesUsed = elixirs.SelectMany(e => e.ElixirComposition).Count();
            var mostUsedNote = elixirs
                .SelectMany(e => e.ElixirComposition)
                .GroupBy(ec => ec.Note.Name)
                .OrderByDescending(g => g.Count())
                .Take(topNoteCount)
                .Select(g => new { Note = g.Key, Count = g.Count() })
                .ToList();

            return new
            {
                TotalElixirs = totalElixirs,
                TotalNotesUsed = totalNotesUsed,
                MostUsedNotes = mostUsedNote
            };
        }


        private async Task<object> GetActionHistory(AppUser user, DateTime? startDate, DateTime? endDate)
        {
            var elixirs = user.Elixirs
                .Where(e => (!startDate.HasValue || e.CreationDate >= startDate)
                         && (!endDate.HasValue || e.CreationDate <= endDate))
                .ToList();

            var totalElixirsInPeriod = elixirs.Count;
            var averageElixirsPerDay = totalElixirsInPeriod / ((endDate ?? DateTime.UtcNow) - (startDate ?? user.Joined)).TotalDays;

            return new
            {
                TotalElixirsInPeriod = totalElixirsInPeriod,
                AverageElixirsPerDay = averageElixirsPerDay
            };
        }

        private async Task<object> GetInteractiveInfo(AppUser user, DateTime? startDate, DateTime? endDate)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User cannot be null.");
            }

            // Отримання всіх пристроїв, пов'язаних з історіями, у зазначений період
            var devicesWithHistories = user.Elixirs
                .SelectMany(elixir => elixir.Histories) // Доступ до історій, пов'язаних із еліксирами користувача
                .Where(history => history.Device != null) // Переконатися, що пристрій присутній
                .Select(history => history.Device)
                .Where(device => (!startDate.HasValue || device.DateOfRegistration >= startDate) &&
                                 (!endDate.HasValue || device.DateOfRegistration <= endDate))
                .ToList();

            // Визначення найпопулярнішого пристрою
            var mostPopularDevice = devicesWithHistories
                .GroupBy(device => device.Name) // Групування за іменами пристроїв
                .OrderByDescending(group => group.Count()) // Сортування за кількістю
                .FirstOrDefault()?.Key; // Отримати ім'я найпопулярнішого пристрою

            return new
            {
                MostPopularDevice = mostPopularDevice,
                TotalDevices = devicesWithHistories.Count
            };
        }

        private async Task<object> GetRatings(AppUser user, DateTime? startDate, DateTime? endDate)
        {
            var allUsers = await _userManager.Users.ToListAsync();

            var userRank = allUsers
                .OrderByDescending(u => u.Elixirs.Count(e => (!startDate.HasValue || e.CreationDate >= startDate)
                                                           && (!endDate.HasValue || e.CreationDate <= endDate)))
                .Select((u, index) => new { User = u, Rank = index + 1 })
                .FirstOrDefault(u => u.User.UserName == user.UserName)?.Rank;

            var topNotes = user.Elixirs
                .SelectMany(e => e.ElixirComposition)
                .GroupBy(ec => ec.Note.Name)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            return new
            {
                UserRank = userRank,
                TopNotes = topNotes
            };
        }

        public async Task<Result<byte[]>> GenerateReportPdf(string userNickname, ReportRequest request)
        {
            _logger.LogInformation("Generating PDF report for user {Nickname}", userNickname);

            try
            {
                var reportResult = await GenerateReport(userNickname, request);
                if (!reportResult.IsSuccessful)
                {
                    _logger.LogError("Failed to generate report for user {Nickname}.", userNickname);
                    return new Result<byte[]>(false);
                }

                var reportData = reportResult.Data;

                // MemoryStream to hold the PDF data
                using var memoryStream = new MemoryStream();

                // Initialize PdfWriter with the MemoryStream
                var pdfWriter = new PdfWriter(memoryStream);

                // Initialize PdfDocument with the PdfWriter
                var pdfDocument = new PdfDocument(pdfWriter);

                // Initialize Document with the PdfDocument
                var document = new Document(pdfDocument);

                // Set default font to handle special characters
                PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, PdfEncodings.IDENTITY_H);
                document.SetFont(font);

                // Add a title to the document
                document.Add(new Paragraph("User Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20));

                // Add sections and their data
                foreach (var section in reportData)
                {
                    // Add section title
                    document.Add(new Paragraph(section.Key ?? "No Section Title")
                        .SetFontSize(16));

                    if (section.Value == null)
                    {
                        document.Add(new Paragraph("No data available for this section.")
                            .SetFontSize(12));
                        continue;
                    }

                    if (section.Value is Dictionary<string, object> sectionDetails)
                    {
                        foreach (var detail in sectionDetails)
                        {
                            document.Add(new Paragraph($"{detail.Key}: {detail.Value ?? "No data"}")
                                .SetFontSize(12));
                        }
                    }
                    else if (section.Value is IEnumerable<object> list)
                    {
                        foreach (var item in list)
                        {
                            document.Add(new Paragraph(item?.ToString() ?? "No data")
                                .SetFontSize(12));
                        }
                    }
                    else
                    {
                        document.Add(new Paragraph(section.Value?.ToString() ?? "No data")
                            .SetFontSize(12));
                    }

                    document.Add(new Paragraph("\n"));
                }

                // Close the document to finalize the PDF
                document.Close();

                _logger.LogInformation("PDF report successfully generated for user {Nickname}", userNickname);

                // Return the byte array of the PDF
                return new Result<byte[]>(true, memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error generating PDF report for user {Nickname}. Error: {Message}", userNickname, ex.Message);
                return new Result<byte[]>(false);
            }
        }

    }
}
