namespace Server.ViewModels.Preferences
{
    public class CreatePreferencesModel
    {
        public string Categories { get; set; } // Наприклад, "Свіжі, Квіткові"
        public string Season { get; set; } // Наприклад, "Літо"
        public string Intensity { get; set; } // Наприклад, "Легкий"
        public string TimeOfDay { get; set; } // Наприклад, "Вечір"
        public string LikedNotes { get; set; } // Наприклад, "Лаванда, Ваніль"
        public string DislikedNotes { get; set; } // Наприклад, "Мускус"
        public string? UserId { get; set; } // Ідентифікатор користувача (null для анонімного користувача)
        public int? ElixirId { get; set; } // ID еліксиру, якщо Preferences пов'язані з еліксиром
    }
}
