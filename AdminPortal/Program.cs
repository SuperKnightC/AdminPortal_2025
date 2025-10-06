using Microsoft.AspNetCore.Mvc;
using AdminPortal.Data;

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

// Add MVC services
builder.Services.AddControllersWithViews();



var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable CSRF protection check globally for MVC controllers
app.UseAntiforgery();

app.UseAuthorization();

// MVC route mapping
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
