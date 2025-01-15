using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class AppUser : Microsoft.AspNetCore.Identity.IdentityUser
    {
        [Column(TypeName = "nvarchar(100)")]
        public string FirstName { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string LastName { get; set; }

        public DateTime Joined { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(500)")]
        public string? Bio { get; set; }
        public string Role { get; set; } = "User"; //"Shop", "Admin"
        public string? PendingRoleChange { get; set; }
        public DateTime DateOfBirth { get; set; }
        public char? Sex { get; set; }

        [ForeignKey(nameof(Preferences))]
        public int? PreferencesId { get; set; }
        public virtual Preferences? Preferences { get; set; }

        // Зв’язок із таблицею Elixirs
        public virtual ICollection<Elixir> Elixirs { get; set; } = new List<Elixir>();

    }
}
