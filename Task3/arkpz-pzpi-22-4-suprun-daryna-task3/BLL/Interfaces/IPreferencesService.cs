using Core.Models;

namespace BLL.Interfaces
{
    public interface IPreferencesService
    {
        Task<Result<Preferences>> AddPreferences(Preferences preferences); // Додавання нового опитування
        Task<Result<Preferences>> UpdatePreferences(Preferences updatedPreferences); // Оновлення
        Task<Result> DeletePreferences(int preferencesId); // Видалення
        Task<Result<Preferences>> GetPreferencesByUser(string userId); // Отримати за користувачем
        //Task<Result<Preferences>> GetPreferencesByElixir(int elixirId); // Отримати за еліксиром
    }
}
