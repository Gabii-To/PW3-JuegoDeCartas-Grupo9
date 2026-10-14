using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Truco.Net.Servicios;
using Truco.NET.Data;
using Truco.Net.Logica.Servicios;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Se conecta a DB usando las credenciales necesarias(connection string)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));


//configuracion de autentificacion para login/register mediante IdentityUser
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

//los datos se guardaran en cache, para sesion
builder.Services.AddDistributedMemoryCache();

// inicio y config de session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Duración de la sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//soporte para patron MVC
builder.Services.AddControllersWithViews();
// añade las clases y se usan como singleton, Una sola instancia en el server.
builder.Services.AddSingleton<IHomeService, HomeService>();
builder.Services.AddSingleton<PartidaService>();
// Inicia el motor de vistar Razor
builder.Services.AddRazorPages();
//añade SignalR
builder.Services.AddSignalR();

//construye segun la config previamente establecida
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


// pipeline en donde pasan todas las peticiones http

//si alguien usa http::dominio, se cambia a https::dominio
app.UseHttpsRedirection();

// Permite cargar Bootstrap, CSS y JS desde wwwroot)
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<PartidaHub>("/partidahub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();