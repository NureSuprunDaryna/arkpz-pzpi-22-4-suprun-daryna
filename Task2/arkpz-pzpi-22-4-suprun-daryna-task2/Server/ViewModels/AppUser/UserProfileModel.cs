namespace Server.ViewModels.AppUser
{
    public class UserProfileModel
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Bio { get; set; }
        public string Email { get; set; }
        public DateTime Joined { get; set; }
        public int DevicesCount { get; set; }
        public int ElixirsCount { get; set; }
    }

}
