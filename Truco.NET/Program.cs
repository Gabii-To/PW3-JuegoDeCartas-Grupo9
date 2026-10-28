using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Truco.Net.Logica.Servicios;
using Truco.Net.Repositorio;
using Truco.Net.Servicios;


var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<EscobaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Los datos se guardarán en caché para la sesión
builder.Services.AddDistributedMemoryCache();

// Inicio y configuración de sesión
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Duración de la sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Soporte para patrón MVC
builder.Services.AddControllersWithViews();

// Servicios personalizados
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddSingleton<PartidaService>();

// Inicia el motor de vistas Razor
builder.Services.AddRazorPages();

// Añade SignalR
builder.Services.AddSignalR();

var app = builder.Build();

// Seeder automático
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<EscobaDbContext>();
        DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos con los usuarios.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
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