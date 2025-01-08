using System.ComponentModel.DataAnnotations;

namespace Server.ViewModels.Elixir
{
    public class GenerateCompositionModel
    {
        [Required]
        [StringLength(1000, ErrorMessage = "Description length can't exceed 1000 characters.")]
        public string Description { get; set; } // Опис еліксиру, який буде використаний для генерації

        public bool IncludePreferences { get; set; } = true; // Чи враховувати вподобання користувача
    }
}
