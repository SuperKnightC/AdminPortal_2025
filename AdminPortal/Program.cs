using AdminPortal.Data;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Logging.AddDebug();
// 1. Retrieve the connection string from configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register DatabaseHelper
// We register DatabaseHelper using a factory function because it requires the connectionString parameter.
// We use AddScoped, meaning one instance is created per request.
builder.Services.AddScoped<DatabaseHelper>(_ => new DatabaseHelper(connectionString));

// 3. Register UserRepository
// UserRepository depends on DatabaseHelper, which is already registered (step 2).
// The DI container will automatically resolve and inject DatabaseHelper into the UserRepository constructor.
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PackageRepository>();
builder.Services.AddScoped<PackageItemRepository>();
builder.Services.AddScoped<AgeCategoryRepository>();
builder.Services.AddScoped<AttractionRepository>();
builder.Services.AddScoped<PackageImageRepository>();
builder.Services.AddScoped<TokenService>();


builder.Services.AddControllersWithViews();// Enable MVC with views support

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // This line is the fix
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FinanceOnly", policy =>
        policy.RequireClaim("department", "FN"));
    options.AddPolicy("CanCreatePackage", policy =>
        policy.RequireClaim("department", "MIS", "TP"));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            // The URL your React app runs on. Vite's default is 5173.
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // allow localhost & ngrok
        options.SaveToken = true;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Token validation failed:");
                Console.WriteLine(context.Exception.ToString());
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine("Authentication challenge triggered.");
                Console.WriteLine($"Error: {context.Error ?? "(none)"}");
                Console.WriteLine($"Description: {context.ErrorDescription ?? "(none)"}");
                Console.WriteLine($"Failure: {context.AuthenticateFailure?.Message ?? "(no failure object)"}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                Console.WriteLine($"Authorization header: {authHeader}");
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            // For development with ngrok, we are keeping these disabled for now
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });
var app = builder.Build(); // Build the app
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
if (!app.Environment.IsDevelopment())  // Configure the HTTP request pipeline for production
{
    app.UseDeveloperExceptionPage();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseRouting(); // Build Route table before executing
app.UseCors("AllowReactApp"); // Enable Frontend to access this API
app.UseAntiforgery(); // Enable Anti-forgery token validation
app.Use(async (context, next) =>
{
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    Console.WriteLine("Authorization Header: " + authHeader);
    await next();
});
app.UseAuthentication(); //Look for JWT token in the request
app.UseAuthorization(); // Check if the user is authorized to access the resource
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");
app.Run();
