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

        public async Task<IActionResult> Index(int patientId, int page = 1)
        {
            var patient = await _context.Patients
                .Include(p => p.Visits)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return NotFound();
            }

            var query = _context.PatientVisits
                .Where(v => v.PatientId == patientId)
                .Include(v => v.TestResults);

            var totalItems = await query.CountAsync();
            var pageSize = 10;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var visits = await query
                .OrderByDescending(v => v.VisitDate)
                .ThenByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PatientName = patient.Name;
            ViewBag.PatientPhone = patient.PhoneNumber;
            ViewBag.PatientId = patient.Id;
            ViewBag.PatientCreatedAt = patient.CreatedAt;
            ViewBag.TotalVisits = patient.Visits.Count;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(visits);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailabilityFromDoctor(int id, string filterDate)
        {
            var visit = await _context.PatientVisits.FindAsync(id);
            if (visit != null)
            {
                visit.IsAvailable = !visit.IsAvailable;
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تحديث حالة الزيارة بنجاح ✓";
            }
            return RedirectToAction(nameof(DoctorVisits), new { date = filterDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVisit(int id, string filterDate)
        {
            var visit = await _context.PatientVisits
                .Include(v => v.TestResults)
                .FirstOrDefaultAsync(v => v.Id == id);
                
            if (visit != null)
            {
                var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                
                // Delete associated test result files
                if (visit.TestResults != null)
                {
                    foreach (var testResult in visit.TestResults)
                    {
                        if (!string.IsNullOrEmpty(testResult.FilePath))
                        {
                            var filePhysicalPath = Path.Combine(webRootPath, testResult.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(filePhysicalPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(filePhysicalPath);
                                }
                                catch (IOException)
                                {
                                    // Log or handle file in use gracefully
                                }
                            }
                        }
                    }
                }
                
                _context.PatientVisits.Remove(visit);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "تم حذف الزيارة وجميع نتائج التحاليل المرتبطة بها بنجاح ✓";
            }
            else
            {
                TempData["Error"] = "لم يتم العثور على الزيارة المطلوبة.";
            }
            
            return RedirectToAction(nameof(DoctorVisits), new { date = filterDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAddVisitFromDoctor(int patientId, DateTime visitDate, string notes, bool isAvailable, string filterDate)
        {
            if (patientId > 0)
            {
                var visit = new PatientVisit
                {
                    PatientId = patientId,
                    VisitDate = visitDate,
                    Notes = notes,
                    IsAvailable = isAvailable
                };
                
                _context.PatientVisits.Add(visit);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "تم إضافة الزيارة بنجاح ✓";
            }
            
            return RedirectToAction(nameof(DoctorVisits), new { date = filterDate });
        }

        [HttpGet]
        public IActionResult AddVisit()
        {
            var model = new AddVisitViewModel { VisitDate = DateTime.Now };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVisit(AddVisitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var phone = model.PhoneNumber.Trim();
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone);

            if (patient == null)
            {
                if (string.IsNullOrWhiteSpace(model.PatientName))
                {
                    ModelState.AddModelError("PatientName", "لم يتم العثور على مريض بهذا الرقم — أدخل اسم المريض لتسجيله كمريض جديد");
                    return View(model);
                }

                patient = new Patient
                {
                    Name = model.PatientName.Trim(),
                    PhoneNumber = phone,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }

            var visit = new PatientVisit
            {
                PatientId = patient.Id,
                VisitDate = model.VisitDate,
                Notes = model.Notes,
                IsAvailable = model.IsAvailable
            };

            _context.PatientVisits.Add(visit);
            await _context.SaveChangesAsync();

            int uploadedCount = 0;
            if (model.Files != null && model.Files.Count > 0)
            {
                foreach (var file in model.Files)
                {
                    if (file.Length > 0 && Path.GetExtension(file.FileName).ToLower() == ".pdf")
                    {
                        var filePath = await _fileUploadService.UploadPdfAsync(file);

                        _context.TestResults.Add(new TestResult
                        {
                            VisitId = visit.Id,
                            TestName = string.IsNullOrWhiteSpace(model.TestName)
                                ? Path.GetFileNameWithoutExtension(file.FileName)
                                : model.TestName,
                            FilePath = filePath
                        });

                        uploadedCount++;
                    }
                }

                await _context.SaveChangesAsync();
            }

            TempData["Success"] = uploadedCount > 0
                ? $"تم تسجيل الزيارة ورفع {uploadedCount} ملف بنجاح ✓"
                : "تم تسجيل الزيارة بنجاح ✓ — يمكنك رفع النتائج لاحقاً";

            return RedirectToAction(nameof(Index), new { patientId = patient.Id });
        }
        [HttpGet]
        public async Task<IActionResult> DoctorVisits(DateTime? date, int page = 1)
        {
            DateTime filterDate = date ?? DateTime.Today;

            var query = _context.PatientVisits
                .Include(v => v.Patient)
                .Include(v => v.TestResults)
                .Where(v => v.VisitDate.Date == filterDate.Date);

            var totalItems = await query.CountAsync();
            var pageSize = 10;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var visits = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.FilterDate = filterDate.ToString("yyyy-MM-dd");

            return View(visits);
        }
    }
}
