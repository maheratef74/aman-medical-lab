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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}