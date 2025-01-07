using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Preferences
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string Categories { get; set; } // Наприклад, "Свіжі, Квіткові"

        [Column(TypeName = "nvarchar(50)")]
        public string Season { get; set; } // Наприклад, "Літо"

        [Column(TypeName = "nvarchar(50)")]
        public string Intensity { get; set; } // Наприклад, "Легкий"

        [Column(TypeName = "nvarchar(50)")]
        public string TimeOfDay { get; set; } // Наприклад, "Вечір"

        [Column(TypeName = "nvarchar(MAX)")]
        public string LikedNotes { get; set; } // Наприклад, "Лаванда, Ваніль"

        [Column(TypeName = "nvarchar(MAX)")]
        public string DislikedNotes { get; set; } // Наприклад, "Мускус"

        // Зовнішній ключ до користувача (необов'язковий для магазину)
        [ForeignKey(nameof(AppUser))]
        public string? UserId { get; set; } // Ідентифікатор користувача (null для анонімного користувача)
        public virtual AppUser? User { get; set; } // Навігаційна властивість
    }
}
