using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpotifyNet.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// En Windows usamos spotifynet.db.
// Docker define ConnectionStrings__DefaultConnection para su volumen persistente.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=spotifynet.db";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Cuenta/IniciarSesion";
    options.AccessDeniedPath = "/Cuenta/IniciarSesion";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

var app = builder.Build();

// Solo el contenedor nuevo aplica migraciones automáticamente.
// No cambia ni reinicializa la base existente de tu PC.
if (builder.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// El Docker local utiliza HTTP. Para exponerlo en Internet,
// coloca HTTPS en un proxy y activa EnableHttpsRedirection según corresponda.
if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("EnableHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
