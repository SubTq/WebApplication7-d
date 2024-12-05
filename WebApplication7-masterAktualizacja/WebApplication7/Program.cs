using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApplication7.Data;

var builder = WebApplication.CreateBuilder(args);

// Konfiguracja Serilog (Logger)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Konfiguracja DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfiguracja uwierzytelniania za pomoc¹ cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

// Dodaj widoki i kontrolery do aplikacji
builder.Services.AddControllersWithViews();

// Pobierz port z zmiennej œrodowiskowej
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";

// Uruchom aplikacjê z ustawieniem dynamicznego portu
var app = builder.Build();

// Obs³uga wyj¹tków (ró¿ni siê w zale¿noœci od trybu)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Obs³uga przekierowania do HTTPS
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Obs³uga uwierzytelniania i autoryzacji
app.UseAuthentication();
app.UseAuthorization();

// Routing kontrolerów
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Dodaj dynamiczne ustawienie URL na podstawie portu
app.Urls.Add($"http://0.0.0.0:{port}");

// Uruchom aplikacjê
app.Run();
