using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Models
{
    public class Device
    {
        public int Id { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string Name { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string Status { get; set; } = "active";
        public DateTime DateOfRegistration { get; set; } = DateTime.UtcNow;

        public virtual ICollection<History> Histories { get; set; } = new List<History>();
    }
}
