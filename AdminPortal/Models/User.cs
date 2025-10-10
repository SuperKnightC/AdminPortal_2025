namespace AdminPortal.Models
{
    //This model responsible for user login variable

    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

}