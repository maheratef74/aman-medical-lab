using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DrMohamedWeb.Controllers
{
    [Authorize]
    public class PatientsController : Controller
    {
        private readonly AmanDbContext _context;

        public PatientsController(AmanDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchPhone)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchPhone))
            {
                query = query.Where(p => p.PhoneNumber.Contains(searchPhone));
            }

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.SearchPhone = searchPhone;

            return View(patients);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (ModelState.IsValid)
            {
                patient.CreatedAt = DateTime.UtcNow;
                _context.Add(patient);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        [HttpGet]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return Json(new { success = false, message = "رقم الهاتف غير صالح" });
            }

            var patients = await _context.Patients
                .Where(p => p.PhoneNumber.Contains(phone))
                .Take(10)
                .Select(p => new { id = p.Id, name = p.Name, phone = p.PhoneNumber })
                .ToListAsync();

            if (patients.Count == 0)
            {
                return Json(new { success = false, message = "لم يتم العثور على مريض بهذا الرقم" });
            }

            return Json(new { success = true, patients = patients });
        }
    }
}
