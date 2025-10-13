using AdminPortal.Data;
using AdminPortal.Models; // Assuming your new models are here
using AdminPortal.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")] 
public class AccountController : ControllerBase
{
    #region-- Repository Declaration --
    private readonly UserRepository _userRepository;
    private readonly TokenService _tokenService;
    #endregion

    #region -- Constructor Injection for UserRepository --
    public AccountController(UserRepository userRepository, TokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }
    #endregion

    #region -- Login Post Method --
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userRepository.GetUserByEmailAsync(model.Email);
        if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            var token = _tokenService.GenerateToken(user);

            // Set the token in an HttpOnly cookie
            Response.Cookies.Append("authToken", token, new CookieOptions
            {
                HttpOnly = true,    // The cookie cannot be accessed by client-side scripts
                Secure = true,      // The cookie will only be sent over HTTPS
                SameSite = SameSiteMode.None, // Required for cross-origin (different ports)
                Expires = DateTime.UtcNow.AddHours(24) // Set an expiration
            });

            return Ok(new { message = "Login successful" });
        }

        return Unauthorized(new { message = "Invalid email or password" });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Clear the cookie by setting an expired one
        Response.Cookies.Delete("authToken", new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None
        });
        return Ok(new { message = "Logged out successfully" });
    }
    #endregion
}