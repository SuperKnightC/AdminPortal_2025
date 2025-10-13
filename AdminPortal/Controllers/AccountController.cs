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
    [HttpPost("login")] // Route: POST /api/account/login
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userRepository.GetUserByEmailAsync(model.Email);
        if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            // In a real API, you would generate a JSON Web Token (JWT) here
            // This token is sent to the front-end and used to authorize future requests
            var token = _tokenService.GenerateToken(user); // Placeholder for token generation
            return Ok(new { token = token });
        }

        // Return a 401 Unauthorized status if login fails
        return Unauthorized(new { message = "Invalid email or password" });
    }
    #endregion
}