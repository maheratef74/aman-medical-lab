using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public AdminController(AmanDbContext context)
        {
            _context = context;
        }

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
                    IsPersistent = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid login attempt.";
            return View();
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

            for (int i = 5; i >= 0; i--)
            {
                var start = thisMonthStart.AddMonths(-i);
                var end = start.AddMonths(1);

                model.MonthLabels.Add(ArabicMonths[start.Month - 1]);
                model.PatientsPerMonth.Add(patients.Count(p => p.CreatedAt >= start && p.CreatedAt < end));
                model.VisitsPerMonth.Add(visits.Count(v => v.VisitDate >= start && v.VisitDate < end));
                model.ResultsPerMonth.Add(results.Count(r => r.UploadedAt >= start && r.UploadedAt < end));
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}