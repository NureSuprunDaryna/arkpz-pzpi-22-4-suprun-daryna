using Core.Models;

namespace BLL.Interfaces
{
    public interface INoteService : IGenericService<Note>
    {
        // Функціональність для користувачів
        Task<Result<List<Note>>> GetAllNotes(); // Перегляд доступних нот
        Task<Result> AddNotesToElixir(int elixirId, List<(int noteId, string noteCategory, decimal proportion)> notesData); // Додавання нот до створених ароматів

        // Функціональність для адміністраторів
        Task<Result<Note>> CreateNote(Note note); // Додавання нової ноти
        Task<Result> EditNote(Note note); // Редагування існуючої ноти
        Task<Result> DeleteNote(int noteId); // Видалення ноти
    }
}
