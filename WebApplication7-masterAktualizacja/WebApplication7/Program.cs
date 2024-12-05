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

// Konfiguracja uwierzytelniania za pomoc� cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

// Dodaj widoki i kontrolery do aplikacji
builder.Services.AddControllersWithViews();

// Zostawienie domy�lnych ustawie� URL (zamiast `UseUrls`) lub upewnienie si�, �e u�ywasz w�a�ciwej domeny
// builder.WebHost.UseUrls("http://*:8080"); // Ewentualnie mo�na usun�� lub dostosowa�

var app = builder.Build();

// Obs�uga wyj�tk�w (r�ni si� w zale�no�ci od trybu)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Obs�uga przekierowania do HTTPS
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Obs�uga uwierzytelniania i autoryzacji
app.UseAuthentication();
app.UseAuthorization();

// Routing kontroler�w
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Uruchom aplikacj�
app.Run();
