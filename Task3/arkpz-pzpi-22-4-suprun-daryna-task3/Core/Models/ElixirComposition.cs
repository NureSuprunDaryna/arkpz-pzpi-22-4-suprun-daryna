using System.ComponentModel.DataAnnotations.Schema;


namespace Core.Models
{
    public class ElixirComposition
    {
        [Column(TypeName = "nvarchar(50)")]
        public string NoteCategory { get; set; } // top, middle, base

        public decimal Proportion { get; set; }

        [ForeignKey(nameof(Elixir))]
        public int ElixirId { get; set; }
        public virtual Elixir Elixir { get; set; }

        [ForeignKey(nameof(Note))]
        public int NoteId { get; set; }
        public virtual Note Note { get; set; }
    }
}
