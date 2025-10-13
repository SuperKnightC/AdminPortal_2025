using AdminPortal.Data;
using AdminPortal.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Use JWT Bearer authentication
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["authToken"];
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // --- Configure how the token will be validated ---

            // 1. Validate the signing key
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),

            // 2. Validate the issuer
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            // 3. Validate the audience
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // 4. Validate the token lifetime
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build(); // Build the app

if (!app.Environment.IsDevelopment())  // Configure the HTTP request pipeline for production
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection(); // Redirect HTTP to HTTPS
app.UseStaticFiles(); // Serve static files from wwwroot
app.UseRouting(); // Build Route table before executing
app.UseCors("AllowReactApp"); // Enable Frontend to access this API
app.UseAntiforgery(); // Enable Anti-forgery token validation
app.UseAuthentication(); //Look for JWT token in the request
app.UseAuthorization(); // Check if the user is authorized to access the resource

// MVC route mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
