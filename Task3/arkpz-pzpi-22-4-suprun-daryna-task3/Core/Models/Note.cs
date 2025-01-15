using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Note
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; }
        public virtual ICollection<ElixirComposition> ElixirComposition { get; set; } = new List<ElixirComposition>();
    }
}
