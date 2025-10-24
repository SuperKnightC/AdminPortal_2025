using be_general_support_api.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace be_general_support_api.Services
{
    //This class represents the authenticated user information used for token generation

    #region-- Token Service for JWT Generation --
    // This service is responsible for generating JWT tokens for authenticated users
    // It uses configuration settings for the JWT key, issuer, and audience
    // The generated token includes claims for user identification and department
    // The token is valid for 24 hours from the time of issuance
    public class TokenService
    {
        private readonly IConfiguration _config;
        public TokenService(IConfiguration config) { _config = config; }

        // Update the method to accept the new AuthUser class
        public string GenerateToken(AuthUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Add the new claims for Department and Name
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("userId", user.AccountId.ToString()),
                new Claim("name", user.Name),
                new Claim("department", user.Department), // <-- NEW CLAIM
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(24),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    #endregion

    #region -- Filter to Prevent Caching --
    //This class is used as an attribute to prevent caching on API responses
    //This mean browser will not store the response, ensuring fresh data on each request 
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, post-check=0, pre-check=0";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";
            base.OnResultExecuting(context);
        }
    }
    #endregion

}