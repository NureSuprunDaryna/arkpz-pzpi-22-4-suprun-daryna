using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Elixir
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;
        public bool IsFavorite { get; set; } = false;
        public List<string> Keywords { get; set; } = new List<string>();

        [ForeignKey(nameof(AppUser))]
        public string AuthorId { get; set; }
        public virtual AppUser Author { get; set; }
        public string Description { get; set; }

        public virtual ICollection<ElixirComposition> ElixirComposition { get; set; } = new List<ElixirComposition>();
        public virtual ICollection<History> Histories { get; set; } = new List<History>();

    }
}
