using be_general_support_api.Data;
using be_general_support_api.Models; 
using be_general_support_api.Services;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    // Route: POST /api/Account/login
    // This endpoint handles user login and JWT token generation
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
        try
        {
            var user = await _userRepository.GetAuthUserByEmailAsync(model.Email);

            if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                var token = _tokenService.GenerateToken(user);
                return Ok(new { token });
            }

            return Unauthorized(new { message = "Invalid email or password" });
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR in Login: " + ex);
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    #endregion

    #region -- Staff Department Get Method --
    // Route: GET /api/Account/me
    // This endpoint retrieves the logged-in user's details from JWT claims
    [Authorize] 
    [HttpGet("me")] 
    public IActionResult GetMe()
    {
        Console.WriteLine(">>> GET /api/Account/me reached");
        var userEmail = User.Claims.FirstOrDefault(c => 
            c.Type == "sub" ||
            c.Type == ClaimTypes.NameIdentifier)?.Value;
        var userName = User.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        var department = User.Claims.FirstOrDefault(c => c.Type == "department")?.Value;

        if (string.IsNullOrEmpty(userEmail))
        {
            return Unauthorized();
        }

        // Return the user's details as JSON
        return Ok(new { email = userEmail, name = userName, department = department });
    }
    #endregion

}