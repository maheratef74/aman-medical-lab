using DrMohamedWeb.Application.Interfaces;
using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AmanDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddSingleton<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();

builder.Services.AddHostedService<ResultFileCleanupService>();
builder.Services.AddScoped<ITokenService, TokenService>();

var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "AmanMedicalLabSecretKeyForJwtAuthentication2026!";
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Admin/Login";
        options.LogoutPath = "/Admin/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                    context.Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new { success = false, message = "Session expired." });
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AmanLabApp",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AmanLabUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Cookies["X-Access-Token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Automatically apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AmanDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();

        // Ensure a default admin account exists so the app is never locked out
        if (!await dbContext.AdminUsers.AnyAsync())
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>();
            dbContext.AdminUsers.Add(new AdminUser
            {
                Email = "admin@amanlab.com",
                FullName = "مدير النظام",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = hasher.HashPassword(new AdminUser(), "strongPassword")
            });
            await dbContext.SaveChangesAsync();
            Console.WriteLine("Default admin account seeded (admin@amanlab.com).");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration exception: {ex.Message}");
    }
}

app.Run();
