using System.ComponentModel.DataAnnotations;

namespace Server.ViewModels.AppUser
{
    public class UpdateRoleModel
    {
        [Required(ErrorMessage = "New role is required.")]
        [StringLength(50, ErrorMessage = "Role name must not exceed 50 characters.")]
        public string NewRole { get; set; }
    }
}
