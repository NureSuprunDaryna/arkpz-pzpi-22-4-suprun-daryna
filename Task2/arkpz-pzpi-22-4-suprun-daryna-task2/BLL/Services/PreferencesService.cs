using BLL.Interfaces;
using Core.Helpers;
using Core.Models;
using DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BLL.Services
{
    public class PreferencesService : GenericService<Preferences>, IPreferencesService
    {
        private readonly IRepository<Preferences> _preferencesRepository;
        private readonly ILogger<PreferencesService> _logger;

        public PreferencesService(UnitOfWork unitOfWork, ILogger<PreferencesService> logger)
                : base(unitOfWork, unitOfWork.PreferencesRepository)
        {
            _preferencesRepository = unitOfWork.PreferencesRepository;
            _logger = logger;
        }


        public async Task<Result<Preferences>> AddPreferences(Preferences preferences)
        {
            _logger.LogInformation("Adding user preferences.");

            if (preferences == null)
            {
                _logger.LogError("Failed to add preferences - input is null.");
                return new Result<Preferences>(false);
            }

            try
            {
                await _preferencesRepository.AddAsync(preferences);
                await _unitOfWork.Save();

                _logger.LogInformation("Preferences added successfully for user {UserId}.", preferences.UserId);
                return new Result<Preferences>(true, preferences);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to add preferences! Error: {ErrorMessage}", ex.Message);
                return new Result<Preferences>(false);
            }
        }

        // Методи для адміністраторів
        public async Task<Result<Preferences>> UpdatePreferences(Preferences updatedPreferences)
        {
            _logger.LogInformation("Updating preferences for user with ID {UserId}.", updatedPreferences?.UserId);

            if (updatedPreferences == null || string.IsNullOrWhiteSpace(updatedPreferences.UserId))
            {
                _logger.LogError("Failed to update preferences - updated preferences model is null or UserId is missing.");
                return new Result<Preferences>(false);
            }

            try
            {
                var existingPreferences = await _preferencesRepository.GetAsync(p => p.UserId == updatedPreferences.UserId);

                if (existingPreferences == null || !existingPreferences.Any())
                {
                    _logger.LogWarning("No preferences found for user {UserId} to update.", updatedPreferences.UserId);
                    return new Result<Preferences>(false);
                }

                var preferencesToUpdate = existingPreferences.FirstOrDefault();

                preferencesToUpdate.MapFrom(updatedPreferences);

                await _preferencesRepository.UpdateAsync(preferencesToUpdate);
                await _unitOfWork.Save();

                _logger.LogInformation("Preferences updated successfully for user {UserId}.", updatedPreferences.UserId);
                return new Result<Preferences>(true, preferencesToUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update preferences for user {UserId}. Error: {ErrorMessage}", updatedPreferences.UserId, ex.Message);
                return new Result<Preferences>(false);
            }
        }

        public async Task<Result> DeletePreferences(int preferencesId)
        {
            _logger.LogInformation("Attempting to delete preferences with ID {PreferencesId}.", preferencesId);

            if (preferencesId <= 0)
            {
                _logger.LogError("Failed to delete preferences - invalid preferences ID.");
                return new Result(false);
            }

            try
            {
                var preferences = await _preferencesRepository.GetByIdAsync(preferencesId);

                if (preferences == null)
                {
                    _logger.LogWarning("Preferences with ID {PreferencesId} not found.", preferencesId);
                    return new Result(false);
                }

                await _preferencesRepository.DeleteAsync(preferences);
                await _unitOfWork.Save();

                _logger.LogInformation("Preferences with ID {PreferencesId} successfully deleted.", preferencesId);
                return new Result(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete preferences with ID {PreferencesId}. Error: {ErrorMessage}", preferencesId, ex.Message);
                return new Result(false);
            }
        }
        /*
        public async Task<Result<Preferences>> GetPreferencesByElixir(int elixirId)
        {
            _logger.LogInformation("Fetching preferences linked to elixir with ID {ElixirId}.", elixirId);

            if (elixirId <= 0)
            {
                _logger.LogError("Failed to fetch preferences - invalid elixir ID.");
                return new Result<Preferences>(false);
            }

            try
            {
                var elixir = await _unitOfWork.ElixirRepository.GetByIdAsync(elixirId);
                if (elixir == null)
                {
                    _logger.LogWarning("Elixir with ID {ElixirId} not found.", elixirId);
                    return new Result<Preferences>(false);
                }

                var preferences = await _preferencesRepository.GetAsync(p => p.ElixirId == elixirId);

                if (preferences == null || !preferences.Any())
                {
                    _logger.LogWarning("No preferences found for elixir with ID {ElixirId}.", elixirId);
                    return new Result<Preferences>(false);
                }

                var preferencesModel = preferences.FirstOrDefault();

                _logger.LogInformation("Preferences successfully retrieved for elixir with ID {ElixirId}.", elixirId);
                return new Result<Preferences>(true, preferencesModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch preferences for elixir with ID {ElixirId}. Error: {ErrorMessage}", elixirId, ex.Message);
                return new Result<Preferences>(false);
            }
        }
        */
        public async Task<Result<Preferences>> GetPreferencesByUser(string userId)
        {
            _logger.LogInformation("Fetching preferences for user {UserId}.", userId);

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogError("Failed to fetch preferences - user ID is null or empty.");
                return new Result<Preferences>(false);
            }

            try
            {
                var preferences = await _preferencesRepository.GetAsync(p => p.UserId == userId);

                if (preferences == null || !preferences.Any())
                {
                    _logger.LogWarning("No preferences found for user {UserId}.", userId);
                    return new Result<Preferences>(false);
                }

                var userPreferences = preferences.FirstOrDefault();

                _logger.LogInformation("Preferences fetched successfully for user {UserId}.", userId);
                return new Result<Preferences>(true, userPreferences);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch preferences for user {UserId}. Error: {ErrorMessage}", userId, ex.Message);
                return new Result<Preferences>(false);
            }
        }

    }
}
