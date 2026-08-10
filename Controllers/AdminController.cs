using DrMohamedWeb.Application.Interfaces;
using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DrMohamedWeb.Controllers
{
    public class AdminController : Controller
    {
        private static readonly string[] ArabicMonths =
        {
            "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        private readonly AmanDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AdminController(AmanDbContext context, ITokenService tokenService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        private bool SecureCookies =>
            Request.IsHttps ||
            string.Equals(Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            // Fixed credentials as requested
            if (email == "admin@amanlab.com" && password == "strongPassword")
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, email),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Generate JWT Access Token (15m) & Refresh Token (7d)
                var accessToken = _tokenService.GenerateAccessToken(email, "Admin");
                var refreshToken = _tokenService.GenerateRefreshToken();
                await _tokenService.SaveRefreshTokenAsync(email, refreshToken);

                // Set HttpOnly Cookies
                Response.Cookies.Append("X-Access-Token", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = SecureCookies,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(15)
                });

                Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = SecureCookies,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
                {
                    return Json(new
                    {
                        success = true,
                        accessToken = accessToken,
                        refreshToken = refreshToken,
                        expiresIn = 900,
                        redirectUrl = Url.Action("Dashboard", "Admin")
                    });
                }

                return RedirectToAction("Dashboard");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Unauthorized(new { success = false, message = "Invalid login attempt." });
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestModel? model)
        {
            var refreshToken = model?.RefreshToken ?? Request.Cookies["X-Refresh-Token"];

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return Unauthorized(new { success = false, message = "Refresh token is missing." });
            }

            var result = await _tokenService.RefreshTokensAsync(refreshToken);

            if (result == null)
            {
                Response.Cookies.Delete("X-Access-Token");
                Response.Cookies.Delete("X-Refresh-Token");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Unauthorized(new { success = false, message = "Invalid or expired refresh token." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@amanlab.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15) });

            Response.Cookies.Append("X-Access-Token", result.Value.newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = SecureCookies,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddMinutes(15)
            });

            Response.Cookies.Append("X-Refresh-Token", result.Value.newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = SecureCookies,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                success = true,
                accessToken = result.Value.newAccessToken,
                refreshToken = result.Value.newRefreshToken,
                expiresIn = 900
            });
        }

        [HttpGet]
        public IActionResult TokenStatus()
        {
            var accessToken = Request.Cookies["X-Access-Token"];
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return Unauthorized(new { success = false, message = "No access token." });
            }

            try
            {
                var secretKey = _config["Jwt:SecretKey"] ?? "AmanMedicalLabSecretKeyForJwtAuthentication2026!";
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

                var principal = new JwtSecurityTokenHandler().ValidateToken(accessToken,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateIssuer = true,
                        ValidIssuer = _config["Jwt:Issuer"] ?? "AmanLabApp",
                        ValidateAudience = true,
                        ValidAudience = _config["Jwt:Audience"] ?? "AmanLabUsers",
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    }, out var validatedToken);

                var exp = (validatedToken as JwtSecurityToken)?.ValidTo ?? DateTime.UtcNow.AddSeconds(-1);
                return Ok(new { success = true, exp = new DateTimeOffset(exp).ToUnixTimeSeconds() });
            }
            catch
            {
                return Unauthorized(new { success = false, message = "Invalid access token." });
            }
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var patients = await _context.Patients.ToListAsync();
            var visits = await _context.PatientVisits.ToListAsync();
            var results = await _context.TestResults.ToListAsync();

            var model = new DashboardViewModel
            {
                TotalPatients = patients.Count,
                TotalVisits = visits.Count,
                TotalTestResults = results.Count,
                NewPatientsThisMonth = patients.Count(p => p.CreatedAt >= thisMonthStart),
                NewPatientsLastMonth = patients.Count(p => p.CreatedAt >= lastMonthStart && p.CreatedAt < thisMonthStart),
                VisitsThisMonth = visits.Count(v => v.VisitDate >= thisMonthStart.Date),
                VisitsLastMonth = visits.Count(v => v.VisitDate >= lastMonthStart.Date && v.VisitDate < thisMonthStart.Date),
                AvailableVisits = visits.Count(v => v.IsAvailable),
                HiddenVisits = visits.Count(v => !v.IsAvailable)
            };

            // ── 6 months ──
            for (int i = 5; i >= 0; i--)
            {
                var start = thisMonthStart.AddMonths(-i);
                var end = start.AddMonths(1);

                model.MonthLabels.Add(ArabicMonths[start.Month - 1]);
                model.PatientsPerMonth.Add(patients.Count(p => p.CreatedAt >= start && p.CreatedAt < end));
                model.VisitsPerMonth.Add(visits.Count(v => v.VisitDate >= start && v.VisitDate < end));
                model.ResultsPerMonth.Add(results.Count(r => r.UploadedAt >= start && r.UploadedAt < end));
            }

            // ── Last 30 days in 5 weekly buckets (W-4, W-3, W-2, W-1, هذا الأسبوع) ──
            var today = now.Date;
            var startOf30 = today.AddDays(-29);
            var arabicWeeks = new[] { "الأسبوع 1", "الأسبوع 2", "الأسبوع 3", "الأسبوع 4", "هذا الأسبوع" };
            for (int w = 0; w < 5; w++)
            {
                var ws = startOf30.AddDays(w * 7);
                var we = ws.AddDays(7);
                if (we > today.AddDays(1)) we = today.AddDays(1);

                model.Labels30Days.Add(arabicWeeks[w]);
                model.Patients30Days.Add(patients.Count(p => p.CreatedAt.Date >= ws && p.CreatedAt.Date < we));
                model.Visits30Days.Add(visits.Count(v => v.VisitDate.Date >= ws && v.VisitDate.Date < we));
                model.Results30Days.Add(results.Count(r => r.UploadedAt.Date >= ws && r.UploadedAt.Date < we));
            }

            // ── Last 7 days daily ──
            var arabicDays = new[] { "الأحد", "الإثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت" };
            for (int d = 6; d >= 0; d--)
            {
                var day = today.AddDays(-d);
                model.Labels7Days.Add(arabicDays[(int)day.DayOfWeek]);
                model.Patients7Days.Add(patients.Count(p => p.CreatedAt.Date == day));
                model.Visits7Days.Add(visits.Count(v => v.VisitDate.Date == day));
                model.Results7Days.Add(results.Count(r => r.UploadedAt.Date == day));
            }

            model.RecentVisits = await _context.PatientVisits
                .OrderByDescending(v => v.VisitDate)
                .ThenByDescending(v => v.Id)
                .Take(5)
                .Select(v => new RecentVisitItem
                {
                    VisitId = v.Id,
                    PatientId = v.PatientId,
                    PatientName = v.Patient != null ? v.Patient.Name : "—",
                    VisitDate = v.VisitDate,
                    FilesCount = v.TestResults.Count,
                    IsAvailable = v.IsAvailable
                })
                .ToListAsync();

            model.RecentPatients = await _context.Patients
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentPatientItem
                {
                    Id = p.Id,
                    Name = p.Name,
                    PhoneNumber = p.PhoneNumber,
                    CreatedAt = p.CreatedAt,
                    VisitsCount = p.Visits.Count
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _tokenService.RevokeRefreshTokenAsync(refreshToken);
            }

            Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Refresh-Token");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}