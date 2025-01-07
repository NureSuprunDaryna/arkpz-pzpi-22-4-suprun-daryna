namespace Server.ViewModels.AppUser
{
    public class RegisterAdminModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; } // "DBAdmin" or "Admin"
        public string Password { get; set; }
    }

}
