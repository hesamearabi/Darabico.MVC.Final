using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using final_p_01.Infrastructure.Data;
using final_p_01.Domain.Entities;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Hosting / reverse proxy: trust only explicitly configured proxies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    var trustedProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    foreach (var proxy in trustedProxies)
    {
        if (IPAddress.TryParse(proxy, out var ip))
        {
            options.KnownProxies.Add(ip);
        }
    }
});

// Performance: response compression.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

// SEO: lowercase URLs without trailing slash.
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.AppendTrailingSlash = false;
});

// Security: HSTS for HTTPS production deployments.
builder.Services.AddHsts(options =>
{
    options.Preload = false;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddControllersWithViews();
builder.Services.AddOutputCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DefaultConnection is missing. Configure the production database connection before starting the application.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        b => b.MigrationsAssembly("final_p_01.Infrastructure")
    ));

// Authentication cookie.
builder.Services.AddAuthentication("DarabicoAuth")
    .AddCookie("DarabicoAuth", options =>
    {
        options.LoginPath = "/Admin/Auth/Login";
        options.LogoutPath = "/Admin/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

// Optional initial-admin provisioning. Disabled by default in production.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    var seedAdminOnStartup = configuration.GetValue<bool>("AdminSettings:SeedOnStartup");

    if (seedAdminOnStartup)
    {
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            if (!context.AdminUsers.Any())
            {
                var adminEmail = configuration["AdminSettings:DefaultEmail"];
                var adminPassword = configuration["AdminSettings:DefaultPassword"];

                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException(
                        "AdminSettings:DefaultEmail and AdminSettings:DefaultPassword are required when AdminSettings:SeedOnStartup is enabled.");
                }

                var passwordHasher = new PasswordHasher<AdminUser>();
                var defaultAdmin = new AdminUser
                {
                    Email = adminEmail
                };

                defaultAdmin.Password = passwordHasher.HashPassword(defaultAdmin, adminPassword);
                context.AdminUsers.Add(defaultAdmin);
                context.SaveChanges();

                logger.LogInformation("Initial admin account was provisioned successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Application startup failed during initial admin provisioning.");
            throw;
        }
    }
}

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
    }
});

app.UseRouting();
app.UseOutputCache();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Posts}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();