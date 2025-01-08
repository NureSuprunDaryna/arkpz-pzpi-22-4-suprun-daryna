using System.ComponentModel.DataAnnotations;

namespace Server.ViewModels.Elixir
{
    public class UpdateCompositionModel
    {
        [Required]
        public int NoteId { get; set; } // Ідентифікатор ноти

        [Required]
        [StringLength(50, ErrorMessage = "Note category length can't exceed 50 characters.")]
        public string NoteCategory { get; set; } // Категорія ноти (наприклад, "top", "middle", "base")

        [Required]
        [Range(0.0, 1.0, ErrorMessage = "Proportion must be between 0.0 and 1.0.")]
        public decimal Proportion { get; set; } // Пропорція ноти в композиції
    }
}
