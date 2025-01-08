using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using DAL.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class ElixirService : GenericService<Elixir>, IElixirService
    {
        private readonly ILogger<ElixirService> _logger;
        private readonly IRepository<Elixir> _elixirRepository;
        private readonly IRepository<Note> _noteRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ElixirService(UnitOfWork unitOfWork,
                             ILogger<ElixirService> logger,
                             UserManager<AppUser> userManager,
                             IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, unitOfWork.ElixirRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _elixirRepository = unitOfWork.ElixirRepository;
            _noteRepository = unitOfWork.NoteRepository;
        }

        public async Task<Result<Elixir>> CreateElixir(Elixir elixir)
        {
            _logger.LogInformation("Creating a new elixir.");

            if (elixir == null)
            {
                _logger.LogError("Failed to create elixir - input is null.");
                return new Result<Elixir>(false);
            }

            try
            {
                elixir.CreationDate = DateTime.UtcNow;

                var preferences = await _unitOfWork.PreferencesRepository
                    .GetAsync(p => p.UserId == elixir.AuthorId);

                if (preferences == null || !preferences.Any())
                {
                    _logger.LogError("Failed to create elixir - Preferences not found for user ID {AuthorId}.", elixir.AuthorId);
                    return new Result<Elixir>(false);
                }

                var availableNotes = await _noteRepository.GetAsync();

                var compositions = SelectNotesForElixir(preferences.First(), availableNotes.ToList());

                await _elixirRepository.AddAsync(elixir);
                await _unitOfWork.Save();

                var updateResult = await UpdateElixirComposition(elixir.Id, compositions);
                if (!updateResult.IsSuccessful)
                {
                    _logger.LogError("Failed to update composition for elixir ID {ElixirId}.", elixir.Id);
                    return new Result<Elixir>(false);
                }

                _logger.LogInformation("Elixir created successfully with ID {Id}", elixir.Id);
                return new Result<Elixir>(true, elixir);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create elixir! Error: {errorMessage}, Inner Exception: {innerException}", ex.Message, ex.InnerException?.Message);
                return new Result<Elixir>(false);
            }
        }

        public async Task<Result<List<Elixir>>> GetElixirs()
        {
            _logger.LogInformation("Fetching all elixirs.");

            try
            {
                var elixirs = await _elixirRepository.GetAsync();
                return new Result<List<Elixir>>(true, elixirs.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch elixirs. Error: {ErrorMessage}", ex.Message);
                return new Result<List<Elixir>>(false);
            }
        }
        public async Task<Result> UpdateElixir(Elixir elixir)
        {
            _logger.LogInformation("Editing elixir with ID {Id}", elixir.Id);

            if (elixir == null || elixir.Id <= 0)
            {
                _logger.LogError("Failed to edit elixir - input is null or invalid ID.");
                return new Result(false);
            }

            try
            {
                var existingElixir = await _elixirRepository.GetByIdAsync(elixir.Id);
                if (existingElixir == null)
                {
                    _logger.LogWarning("Elixir with ID {Id} not found.", elixir.Id);
                    return new Result(false);
                }

                existingElixir.MapFrom(elixir);

                await _elixirRepository.UpdateAsync(existingElixir);
                await _unitOfWork.Save();

                _logger.LogInformation("Elixir with ID {Id} updated successfully.", elixir.Id);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to edit elixir with ID {Id}. Error: {errorMessage}", elixir.Id, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> DeleteElixir(int elixirId)
        {
            _logger.LogInformation("Deleting elixir with ID {Id}", elixirId);

            if (elixirId <= 0)
            {
                _logger.LogError("Failed to delete elixir - invalid ID provided.");
                return new Result(false);
            }

            try
            {
                var elixir = await _elixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogWarning("Elixir with ID {Id} not found.", elixirId);
                    return new Result(false);
                }

                await _elixirRepository.DeleteAsync(elixir);
                await _unitOfWork.Save();

                _logger.LogInformation("Elixir with ID {Id} deleted successfully.", elixirId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete elixir with ID {Id}. Error: {errorMessage}", elixirId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result> SaveFavoriteElixir(int elixirId)
        {
            _logger.LogInformation("Saving elixir with ID {Id} to favorites.", elixirId);

            if (elixirId <= 0)
            {
                _logger.LogError("Failed to save elixir to favorites - invalid elixir ID.");
                return new Result(false);
            }

            try
            {
                var userId = _httpContextAccessor.HttpContext?.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Failed to save elixir to favorites - user is not authenticated.");
                    return new Result(false);
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("User with ID {UserId} not found.", userId);
                    return new Result(false);
                }

                var elixir = await _elixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogWarning("Elixir with ID {Id} not found.", elixirId);
                    return new Result(false);
                }

                if (elixir.IsFavorite)
                {
                    _logger.LogWarning("Elixir with ID {Id} is already in the favorites for user {UserId}.", elixirId, userId);
                    return new Result(false);
                }

                elixir.IsFavorite = true;
                user.Elixirs.Add(elixir);

                await _unitOfWork.Save();

                _logger.LogInformation("Elixir with ID {Id} added to favorites for user {UserId}.", elixirId, userId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save elixir with ID {Id} to favorites. Error: {errorMessage}", elixirId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result<List<Elixir>>> GetElixirsByAuthor(string authorId)
        {
            var elixirs = await _elixirRepository.GetAsync(e => e.AuthorId == authorId);
            return new Result<List<Elixir>>(true, elixirs.ToList());
        }

        /*
        private List<(string Note, decimal Proportion, string Category)> GenerateProportions(List<string> notes, string intensity)
        {
            // Розподіл залежно від інтенсивності
            decimal topProportion = 0.0m;
            decimal middleProportion = 0.0m;
            decimal baseProportion = 0.0m;

            switch (intensity.ToLower())
            {
                case "легкий":
                    topProportion = 0.6m;
                    middleProportion = 0.3m;
                    baseProportion = 0.1m;
                    break;
                case "середній":
                    topProportion = 0.4m;
                    middleProportion = 0.4m;
                    baseProportion = 0.2m;
                    break;
                case "сильний":
                    topProportion = 0.2m;
                    middleProportion = 0.3m;
                    baseProportion = 0.5m;
                    break;
                default:
                    topProportion = 0.4m;
                    middleProportion = 0.4m;
                    baseProportion = 0.2m;
                    break;
            }

            // Розподіл нот за категоріями
            var result = new List<(string Note, decimal Proportion, string Category)>();

            foreach (var note in notes)
            {
                string category = DetermineCategory(note); // Метод для визначення категорії ноти
                decimal proportion = category switch
                {
                    "top" => topProportion / notes.Count(n => DetermineCategory(n) == "top"),
                    "middle" => middleProportion / notes.Count(n => DetermineCategory(n) == "middle"),
                    "base" => baseProportion / notes.Count(n => DetermineCategory(n) == "base"),
                    _ => 0.0m
                };

                result.Add((note, proportion, category));
            }

            return result;
        }

        public async Task<List<(string Note, decimal Proportion, string Category)>> GenerateElixirComposition(string userId, string description)
        {
            // Отримати вподобання користувача
            var preferences = await _preferencesService.GetPreferencesByUserId(userId);
            if (!preferences.IsSuccessful || preferences.Data == null)
            {
                throw new Exception("Failed to retrieve user preferences.");
            }

            // Відправка запиту до AI
            var aiResponse = await _aiService.GenerateComposition(preferences.Data, description);

            // Обробка відповіді від AI
            var notes = aiResponse.Notes; // Ноти від AI
            var intensity = preferences.Data.Intensity; // Інтенсивність від вподобань

            // Генерація пропорцій
            return GenerateProportions(notes, intensity);
        }

        public async Task<Result<Elixir>> GenerateElixirComposition(string userId, string description)
        {
            try
            {
                var composition = await GenerateElixirComposition(userId, description);

                var newElixir = new Elixir
                {
                    Name = "Custom Elixir",
                    AuthorId = userId,
                    ElixirComposition = composition.Select(c => new ElixirComposition
                    {
                        NoteId = c.NoteId,
                        NoteCategory = c.Category,
                        Proportion = c.Proportion
                    }).ToList()
                };

                await _elixirRepository.AddAsync(newElixir);
                await _unitOfWork.Save();

                return new Result<Elixir>(true, newElixir);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate elixir composition. Error: {ErrorMessage}", ex.Message);
                return new Result<Elixir>(false);
            }
        }

        /// ///////////////////////////////////////////////////////////////////////////////////////
        public async Task<Result<List<Elixir>>> GetRecommendedElixirs(string userNickname)
        {
            _logger.LogInformation("Generating AI-based recommendations for user {UserNickname}.", userNickname);

            if (string.IsNullOrWhiteSpace(userNickname))
            {
                _logger.LogError("Failed to generate recommendations - user nickname is null or empty.");
                return new Result<List<Elixir>>(false);
            }

            try
            {
                var user = await _userManager.FindByNameAsync(userNickname);
                if (user == null)
                {
                    _logger.LogError("User with nickname {UserNickname} not found.", userNickname);
                    return new Result<List<Elixir>>(false);
                }

                var userElixirs = await _elixirRepository.GetAsync(
                    filter: e => e.AuthorId == userNickname);

                if (!userElixirs.Any())
                {
                    _logger.LogWarning("No elixirs found for user {UserNickname}.", userNickname);
                    return new Result<List<Elixir>>(true, new List<Elixir>());
                }

                var recommendedElixirs = await GenerateRecommendations(userElixirs);

                _logger.LogInformation("Successfully generated {Count} recommendations for user {UserNickname}.",
                    recommendedElixirs.Count, userNickname);

                return new Result<List<Elixir>>(true, recommendedElixirs.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate recommendations for user {UserNickname}. Error: {ErrorMessage}",
                    userNickname, ex.Message);
                return new Result<List<Elixir>>(false);
            }
        }

        private async Task<List<Elixir>> GenerateRecommendations(IEnumerable<Elixir> userElixirs)
        {
            // Отримання всіх еліксирів, крім створених користувачем
            var allElixirs = await _elixirRepository.GetAsync();
            var otherElixirs = allElixirs.Where(e => !userElixirs.Select(ue => ue.Id).Contains(e.Id)).ToList();

            // Тут буде інтеграція з AI для рекомендацій
            // Простий приклад на основі подібності за кількістю однакових нот
            var recommendations = otherElixirs
                .OrderByDescending(e => e.ElixirComposition.Count(ec =>
                    userElixirs.SelectMany(ue => ue.ElixirComposition).Select(uc => uc.NoteId).Contains(ec.NoteId)))
                .Take(7)
                .ToList();

            return recommendations;
        }
        */
        //////////////////////////////////////////////////////////////////////////////////////////////////

        public async Task<Result> UpdateElixirComposition(int elixirId, List<(int noteId, string noteCategory, decimal proportion)> updatedNotes)
        {
            _logger.LogInformation("Updating composition for elixir with ID {ElixirId}.", elixirId);

            try
            {
                var elixir = await _elixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogError("Elixir with ID {ElixirId} not found.", elixirId);
                    return new Result(false);
                }

                foreach (var note in updatedNotes)
                {
                    var existingNote = elixir.ElixirComposition.FirstOrDefault(ec => ec.NoteId == note.noteId);
                    if (existingNote != null)
                    {
                        existingNote.NoteCategory = note.noteCategory;
                        existingNote.Proportion = note.proportion;
                    }
                    else
                    {
                        elixir.ElixirComposition.Add(new ElixirComposition
                        {
                            ElixirId = elixirId,
                            NoteId = note.noteId,
                            NoteCategory = note.noteCategory,
                            Proportion = note.proportion
                        });
                    }
                }

                await _unitOfWork.Save();
                _logger.LogInformation("Successfully updated composition for elixir with ID {ElixirId}.", elixirId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update composition for elixir with ID {ElixirId}. Error: {ErrorMessage}", elixirId, ex.Message);
                return new Result(false);
            }
        }

        public async Task<Result<List<Note>>> GetElixirComposition(int elixirId)
        {
            _logger.LogInformation("Retrieving composition for elixir with ID {ElixirId}.", elixirId);

            try
            {
                var elixir = await _elixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogError("Elixir with ID {ElixirId} not found.", elixirId);
                    return new Result<List<Note>>(false);
                }

                var notes = elixir.ElixirComposition
                    .Select(ec => ec.Note)
                    .ToList();

                _logger.LogInformation("Successfully retrieved composition for elixir with ID {ElixirId}.", elixirId);
                return new Result<List<Note>>(true, notes);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve composition for elixir with ID {ElixirId}. Error: {ErrorMessage}", elixirId, ex.Message);
                return new Result<List<Note>>(false);
            }
        }

        public async Task<Result<List<Elixir>>> ModerateElixirs()
        {
            _logger.LogInformation("Retrieving elixirs for moderation.");

            try
            {
                // Отримуємо всі еліксири, які потребують модерації
                var elixirsToModerate = await _elixirRepository.GetAsync(e =>
                    string.IsNullOrWhiteSpace(e.Name) || // Відсутність назви
                    e.Description == null || // Відсутність опису
                    e.ElixirComposition.Count < 3); // Менше трьох нот у складі

                if (!elixirsToModerate.Any())
                {
                    _logger.LogInformation("No elixirs require moderation.");
                    return new Result<List<Elixir>>(true, new List<Elixir>());
                }

                _logger.LogInformation("Successfully retrieved {Count} elixirs for moderation.", elixirsToModerate.Count);
                return new Result<List<Elixir>>(true, elixirsToModerate.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to retrieve elixirs for moderation. Error: {ErrorMessage}", ex.Message);
                return new Result<List<Elixir>>(false);
            }
        }

        public async Task<Result> DeleteElixirByAdmin(int elixirId)
        {
            _logger.LogInformation("Admin is attempting to delete elixir with ID {ElixirId}.", elixirId);

            try
            {
                var elixir = await _elixirRepository.GetByIdAsync(elixirId);

                if (elixir == null)
                {
                    _logger.LogWarning("Elixir with ID {ElixirId} not found.", elixirId);
                    return new Result(false);
                }

                await _elixirRepository.DeleteAsync(elixir);
                await _unitOfWork.Save();

                _logger.LogInformation("Elixir with ID {ElixirId} successfully deleted by admin.", elixirId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete elixir with ID {ElixirId}. Error: {ErrorMessage}", elixirId, ex.Message);
                return new Result(false);
            }
        }

        private List<(int noteId, string noteCategory, decimal proportion)> SelectNotesForElixir(Preferences preferences, List<Note> availableNotes)
        {
            // Отримуємо вподобання
            var likedNotes = preferences.LikedNotes?.Split(", ").ToList() ?? new List<string>();
            var dislikedNotes = preferences.DislikedNotes?.Split(", ").ToList() ?? new List<string>();

            // Фільтруємо доступні ноти
            availableNotes = availableNotes
                .Where(note => !dislikedNotes.Contains(note.Name)) // Видаляємо disliked
                .OrderBy(note => likedNotes.Contains(note.Name) ? 0 : 1) // Пріоритет liked
                .ToList();

            // Пропорції залежно від Intensity
            var proportions = GetProportionsBasedOnIntensity(preferences.Intensity);

            // Списки для категорій
            var topNotes = new List<Note>();
            var middleNotes = new List<Note>();
            var baseNotes = new List<Note>();

            // Розподіл нот між категоріями
            for (int i = 0; i < availableNotes.Count; i++)
            {
                var note = availableNotes[i];

                if (topNotes.Count < 2) topNotes.Add(note);
                else if (middleNotes.Count < 2) middleNotes.Add(note);
                else if (baseNotes.Count < 2) baseNotes.Add(note);
            }

            // Формування композицій
            var compositions = new List<(int noteId, string noteCategory, decimal proportion)>();
            AddCompositionsForCategory(topNotes, "Top", proportions["Top"], compositions);
            AddCompositionsForCategory(middleNotes, "Middle", proportions["Middle"], compositions);
            AddCompositionsForCategory(baseNotes, "Base", proportions["Base"], compositions);

            return compositions;
        }

        // Визначення пропорцій залежно від Intensity
        private Dictionary<string, decimal> GetProportionsBasedOnIntensity(string intensity)
        {
            return intensity switch
            {
                "Легкий" => new Dictionary<string, decimal>
        {
            { "Top", 0.5m },
            { "Middle", 0.3m },
            { "Base", 0.2m }
        },
                "Середній" => new Dictionary<string, decimal>
        {
            { "Top", 0.4m },
            { "Middle", 0.3m },
            { "Base", 0.3m }
        },
                "Інтенсивний" => new Dictionary<string, decimal>
        {
            { "Top", 0.3m },
            { "Middle", 0.3m },
            { "Base", 0.4m }
        },
                _ => new Dictionary<string, decimal>
        {
            { "Top", 0.4m },
            { "Middle", 0.3m },
            { "Base", 0.3m }
        }
            };
        }

        // Додавання композицій у категорії
        private void AddCompositionsForCategory(List<Note> notes, string category, decimal categoryProportion, List<(int, string, decimal)> compositions)
        {
            if (notes.Count == 0) return;

            var proportionPerNote = categoryProportion / notes.Count;

            foreach (var note in notes)
            {
                compositions.Add((note.Id, category, proportionPerNote));
            }
        }

    }
}
