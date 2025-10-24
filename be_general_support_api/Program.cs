using be_general_support_api.Data;
using be_general_support_api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// 1. Retrieve the connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Register DatabaseHelper
builder.Services.AddScoped<DatabaseHelper>(_ => new DatabaseHelper(connectionString));

// 3. Register Repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PackageRepository>();
builder.Services.AddScoped<PackageItemRepository>();
builder.Services.AddScoped<AgeCategoryRepository>();
builder.Services.AddScoped<AttractionRepository>();
builder.Services.AddScoped<PackageImageRepository>();
builder.Services.AddScoped<TokenService>();

// 4. Add MVC Controllers
builder.Services.AddControllersWithViews();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Fix JSON serialization to use camelCase
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 5. Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("FinanceOnly", policy =>
        policy.RequireClaim("department", "FN"));
    options.AddPolicy("CanCreatePackage", policy =>
        policy.RequireClaim("department", "MIS", "TP"));
    options.AddPolicy("TPOnly", policy =>
        policy.RequireClaim("department", "TP"));
});

// 6. Add CORS for React App
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // React app URL
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// 7. Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Allow localhost & ngrok
        options.SaveToken = true;

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // You can add logging here if needed
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                // You can add logging here if needed
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            ValidateIssuer = false,  // For development
            ValidateAudience = false // For development
        };
    });

var app = builder.Build();

// --- Configure the HTTP request pipeline ---

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Use developer exception page in development, otherwise use error handler
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(); // Serve static files from wwwroot
app.UseRouting();
app.UseCors("AllowReactApp"); // Enable Frontend to access this API

app.UseAuthentication(); // Look for JWT token
app.UseAuthorization(); // Check if the user is authorized

app.MapControllers(); // Map API controllers

app.Run();