using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SaintHub.Data;
using SaintHub.Services;


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

// =========================
// Email + alertas de pedidos
// (si no configurás SMTP/recipients, simplemente no envía y NO se cae)
// =========================
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<OrderAlertsOptions>(builder.Configuration.GetSection("OrderAlerts"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IOrderAlertService, OrderAlertService>();

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

// Render / reverse proxies: confiar en Forwarded Headers para que HTTPS/redirecciones + URLs funcionen bien
var fwd = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
};
fwd.KnownNetworks.Clear();
fwd.KnownProxies.Clear();
app.UseForwardedHeaders(fwd);

// =========================
// PIPELINE
// =========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
var staticFileProvider = new FileExtensionContentTypeProvider();
staticFileProvider.Mappings[".webp"] = "image/webp";
staticFileProvider.Mappings[".avif"] = "image/avif";
// Algunos clientes suben extensiones en mayúscula (Linux es case-sensitive)
staticFileProvider.Mappings[".WEBP"] = "image/webp";
staticFileProvider.Mappings[".AVIF"] = "image/avif";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileProvider
});

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
var uploadsRoot = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
Directory.CreateDirectory(Path.Combine(uploadsRoot, "products"));

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

// disk-test: redeploy should not delete uploads
