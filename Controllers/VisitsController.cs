using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DrMohamedWeb.Controllers
{
    [Authorize]
    public class VisitsController : Controller
    {
        private readonly AmanDbContext _context;

        public VisitsController(AmanDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.Visits)
                .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = patient.Name;
            ViewBag.PatientId = patient.Id;

            return View(patient.Visits.OrderByDescending(v => v.VisitDate));
        }

        [HttpGet]
        public IActionResult Create(int patientId)
        {
            var visit = new PatientVisit { PatientId = patientId, VisitDate = DateTime.Today };
            return View(visit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientVisit visit)
        {
            if (ModelState.IsValid)
            {
                _context.Add(visit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { patientId = visit.PatientId });
            }
            return View(visit);
        }
    }
}
