namespace be_general_support_api.Models
{
    //This model responsible for user login variable

    #region -- User Login View Model --
    public class AuthUser
    {
        public int AccountId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Name { get; set; }
        public string Department { get; set; }
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