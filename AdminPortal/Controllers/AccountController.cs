using Microsoft.AspNetCore.Mvc; //model view controller
using BCrypt.Net; //for hashing 
using AdminPortal.Data; //access userrepository

public class AccountController : Controller //give basic mvc functionality 
{
    private readonly UserRepository _userRepository; //hold an instance of user repository 
    public AccountController(UserRepository userRepository) //constructor, dependency injection provide by asp.net core framework
    {
        _userRepository = userRepository;
    }

    [HttpGet] //method below only response for get
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken] //security to ensure it contain secret token generated in the html
    public async Task<IActionResult> Register (string email, string password) //receive email and password
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(password);//hashing password
        await _userRepository.AddUserAsync(email,hashed);//call to save the credential
        return RedirectToAction("Login");//return to original page
    }

    [HttpGet]

    public IActionResult Login() => View(); //Index view (/Views/Account/Index.cshtml).

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email,string password) //receive credential
    {
        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user != null) // First, check if a user was found
        {
            // Now that we know 'user' is not null, we can safely check the password
            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return RedirectToAction("Homepage");
            }
        }
        return RedirectToAction("Login");
    }
    [HttpGet] // New action name: Homepage
    public IActionResult Homepage() => View();

}