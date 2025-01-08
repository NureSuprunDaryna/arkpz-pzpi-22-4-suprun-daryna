using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class History
    {
        public int Id { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(50)")]
        public string ExecutionStatus { get; set; } // Наприклад, success, failure

        [ForeignKey(nameof(Device))]
        public int? DeviceId { get; set; }
        public virtual Device Device { get; set; }

        [ForeignKey(nameof(Elixir))]
        public int? ElixirId { get; set; }
        public virtual Elixir Elixir { get; set; }
    }
}
