using Core.Models;

namespace BLL.Interfaces
{
    public interface IElixirService : IGenericService<Elixir>
    {
        // Функціональність для користувачів
        Task<Result<Elixir>> CreateElixir(Elixir elixir); // Створення нового аромату
        Task<Result> UpdateElixir(Elixir elixir); // Редагування створеного аромату
        Task<Result> DeleteElixir(int elixirId); // Видалення аромату
        Task<Result> SaveFavoriteElixir(int elixirId); // Збереження улюбленого аромату
                                                       //
                                                       //
                                                       //Task<Result<List<Elixir>>> GetRecommendedElixirs(string userNickname); // Рекомендації ароматів
        Task<Result<List<Elixir>>> GetElixirsByAuthor(string authorId);

        // Функціональність для складу аромату
        Task<Result> UpdateElixirComposition(int elixirId, List<(int noteId, string noteCategory, decimal proportion)> updatedNotes);
        Task<Result<List<Note>>> GetElixirComposition(int elixirId); // Перегляд складу аромату

        // Функціональність для адміністраторів
        Task<Result<List<Elixir>>> ModerateElixirs(); // Модерація ароматів
        Task<Result> DeleteElixirByAdmin(int elixirId); // Видалення аромату адміністратором
        Task<Result<List<Elixir>>> GetElixirs();
    }
}
