using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

// =========================
// SERVICES
// =========================

builder.Services.AddControllersWithViews();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

var cs = builder.Configuration.GetConnectionString("MySqlConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(cs, ServerVersion.AutoDetect(cs))
);

var app = builder.Build();

// =========================
// PIPELINE
// =========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ?? SESSION PRIMERO
app.UseSession();

// ?? AUTH DESPUÉS
app.UseAuthentication();
app.UseAuthorization();

// =========================
// ADMIN GUARD (SESSION)
// =========================

app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    if (path != null && path.StartsWith("/admin"))
    {
        var isLogged = context.Session.GetString("ADMIN_AUTH") == "OK";

        if (!isLogged && !path.StartsWith("/admin/login"))
        {
            context.Response.Redirect("/Admin/Login");
            return;
        }
    }

    await next();
});

// =========================
// ROUTES
// =========================

app.MapControllerRoute(
    name: "category",
    pattern: "categoria/{category}",
    defaults: new { controller = "Category", action = "Index" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
