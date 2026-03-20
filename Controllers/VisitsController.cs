using DrMohamedWeb.Application.Interfaces;
using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.IO;

namespace DrMohamedWeb.Controllers
{
    [Authorize]
    public class VisitsController : Controller
    {
        private readonly AmanDbContext _context;
        private readonly IFileUploadService _fileUploadService;

        public VisitsController(AmanDbContext context, IFileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
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

        [HttpGet]
        public IActionResult AddByPhone()
        {
            var model = new AddVisitByPhoneViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddByPhone(AddVisitByPhoneViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == model.PhoneNumber);

            if (patient == null)
            {
                ModelState.AddModelError("PhoneNumber", "لم يتم العثور على مريض بهذا الرقم");
                return View(model);
            }

            if (model.Files == null || model.Files.Count == 0)
            {
                ModelState.AddModelError("Files", "يرجى اختيار ملف (PDF) واحد على الأقل");
                return View(model);
            }

            var visit = new PatientVisit
            {
                PatientId = patient.Id,
                VisitDate = model.VisitDate,
                Notes = model.Notes,
                IsAvailable = model.IsAvailable
            };

            _context.PatientVisits.Add(visit);
            await _context.SaveChangesAsync(); // Save to generate VisitId

            bool hasError = false;
            foreach (var file in model.Files)
            {
                if (file.Length > 0 && Path.GetExtension(file.FileName).ToLower() == ".pdf")
                {
                    var filePath = await _fileUploadService.UploadPdfAsync(file);

                    var testResult = new TestResult
                    {
                        VisitId = visit.Id,
                        TestName = model.TestName,
                        FilePath = filePath
                    };

                    _context.TestResults.Add(testResult);
                }
                else
                {
                    ModelState.AddModelError("", $"الملف {file.FileName} ليس ملف PDF صالح.");
                    hasError = true;
                }
            }

            if (hasError)
            {
                // If there are file errors after visit creation, you might want to handle it (e.g., delete the visit, or just show errors and keep the visit). 
                // Opting to save the valid results and return errors for invalid ones.
                await _context.SaveChangesAsync();
                return View(model); 
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { patientId = patient.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id, int patientId)
        {
            var visit = await _context.PatientVisits.FindAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            visit.IsAvailable = !visit.IsAvailable;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { patientId = patientId });
        }
    }
}
