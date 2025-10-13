namespace AdminPortal.Models
{
    //This model responsible for user login variable

    #region -- User Login View Model --
    public class User // For user data representation (DB)
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
    #endregion

    #region -- Login Model for Receiving Data --
    public class LoginModel // For receiving login data from front-end
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    #endregion
}