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

        public async Task<IActionResult> Index(string searchQuery, int page = 1)
        {
            var query = _context.Patients
                .Include(p => p.Visits)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim();
                query = query.Where(p => p.Name.Contains(term) || p.PhoneNumber.Contains(term));
            }

            var totalItems = await query.CountAsync();
            var pageSize = 10;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var patients = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchQuery = searchQuery;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(patients);
        }

        // ──────────────────────────────────────────
        // CREATE
        // ──────────────────────────────────────────
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
                var phone = patient.PhoneNumber?.Trim() ?? "";
                var exists = await _context.Patients.AnyAsync(p => p.PhoneNumber == phone);
                if (exists)
                {
                    ModelState.AddModelError("PhoneNumber", "رقم الهاتف هذا مسجل بالفعل لمريض آخر.");
                    return View(patient);
                }

                patient.PhoneNumber = phone;
                patient.CreatedAt = DateTime.UtcNow;
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"تم إضافة المريض \"{patient.Name}\" بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // ──────────────────────────────────────────
        // EDIT
        // ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (id != patient.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                var phone = patient.PhoneNumber?.Trim() ?? "";
                var exists = await _context.Patients.AnyAsync(p => p.PhoneNumber == phone && p.Id != id);
                if (exists)
                {
                    ModelState.AddModelError("PhoneNumber", "رقم الهاتف هذا مسجل بالفعل لمريض آخر.");
                    return View(patient);
                }

                var existing = await _context.Patients.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Name = patient.Name;
                existing.PhoneNumber = phone;
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تعديل بيانات المريض بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(patient);
        }

        // ──────────────────────────────────────────
        // INLINE EDIT (AJAX — called from modal in Index)
        // ──────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> InlineEdit([FromBody] PatientInlineEditRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Phone))
                return Json(new { success = false, message = "البيانات غير مكتملة" });

            var phone = req.Phone.Trim();
            var exists = await _context.Patients.AnyAsync(p => p.PhoneNumber == phone && p.Id != req.Id);
            if (exists)
                return Json(new { success = false, message = "رقم الهاتف هذا مسجل بالفعل لمريض آخر." });

            var patient = await _context.Patients.FindAsync(req.Id);
            if (patient == null)
                return Json(new { success = false, message = "المريض غير موجود" });

            patient.Name = req.Name.Trim();
            patient.PhoneNumber = phone;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم التعديل بنجاح" });
        }

        // ──────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Visits)
                    .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تم حذف المريض \"{patient.Name}\" بنجاح";
            return RedirectToAction(nameof(Index));
        }

        // ──────────────────────────────────────────
        // SEARCH BY PHONE (autocomplete)
        // ──────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "رقم الهاتف غير صالح" });

            var patients = await _context.Patients
                .Where(p => p.PhoneNumber.Contains(phone))
                .Take(10)
                .Select(p => new { id = p.Id, name = p.Name, phone = p.PhoneNumber })
                .ToListAsync();

            if (patients.Count == 0)
                return Json(new { success = false, message = "لم يتم العثور على مريض بهذا الرقم" });

            return Json(new { success = true, patients = patients });
        }
    }

    // DTO for AJAX inline edit
    public class PatientInlineEditRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
