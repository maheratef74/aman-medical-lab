using DrMohamedWeb.Application.Interfaces;
using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace DrMohamedWeb.Controllers
{
    [Authorize]
    public class TestResultsController : Controller
    {
        private readonly AmanDbContext _context;
        private readonly IFileUploadService _fileUploadService;

        public TestResultsController(AmanDbContext context, IFileUploadService fileUploadService)
        {
            _context = context;
            _fileUploadService = fileUploadService;
        }

        [HttpGet]
        public async Task<IActionResult> Upload(int visitId)
        {
            var visit = await _context.PatientVisits
                .Include(v => v.Patient)
                .FirstOrDefaultAsync(v => v.Id == visitId);

            if (visit == null)
            {
                return NotFound();
            }

            ViewBag.PatientName = visit.Patient?.Name;
            ViewBag.VisitDate = visit.VisitDate.ToString("yyyy-MM-dd");

            var model = new UploadResultViewModel { VisitId = visitId, PatientId = visit.PatientId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(UploadResultViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.Files != null && model.Files.Count > 0)
                {
                    foreach (var file in model.Files)
                    {
                        if (file.Length > 0 && Path.GetExtension(file.FileName).ToLower() == ".pdf")
                        {
                            var filePath = await _fileUploadService.UploadPdfAsync(file);

                            var testResult = new TestResult
                            {
                                VisitId = model.VisitId,
                                TestName = model.TestName,
                                FilePath = filePath
                            };

                            _context.TestResults.Add(testResult);
                        }
                        else
                        {
                            ModelState.AddModelError("", $"File {file.FileName} is not a valid PDF.");
                        }
                    }

                    if (ModelState.IsValid)
                    {
                        await _context.SaveChangesAsync();
                        return RedirectToAction("Index", "Visits", new { patientId = model.PatientId });
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Please select at least one file.");
                }
            }

            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult != null)
            {
                int patientId = _context.PatientVisits.Find(testResult.VisitId)?.PatientId ?? 0;
                
                // Physically delete the PDF file if it exists on the server
                if (!string.IsNullOrEmpty(testResult.FilePath))
                {
                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
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

                _context.TestResults.Remove(testResult);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "تم حذف نتيجة التحليل والملف بنجاح ✓";
                return RedirectToAction("Index", "Visits", new { patientId = patientId });
            }
            return NotFound();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFromDoctor(UploadResultViewModel model, [FromForm] string FilterDate)
        {
            if (model.Files != null && model.Files.Count > 0)
            {
                int uploadedCount = 0;
                foreach (var file in model.Files)
                {
                    if (file.Length > 0 && Path.GetExtension(file.FileName).ToLower() == ".pdf")
                    {
                        var filePath = await _fileUploadService.UploadPdfAsync(file);

                        var testResult = new TestResult
                        {
                            VisitId = model.VisitId,
                            TestName = string.IsNullOrWhiteSpace(model.TestName)
                                ? Path.GetFileNameWithoutExtension(file.FileName)
                                : model.TestName,
                            FilePath = filePath
                        };

                        _context.TestResults.Add(testResult);
                        uploadedCount++;
                    }
                }

                if (uploadedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"تم إضافة {uploadedCount} نتيجة بنجاح ✓";
                }
                else
                {
                    TempData["Success"] = "لم يتم إضافة أي ملفات. يرجى التأكد من اختيار ملفات PDF.";
                }
            }

            return RedirectToAction("DoctorVisits", "Visits", new { date = FilterDate });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromDoctor(int id, [FromForm] string FilterDate)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult != null)
            {
                if (!string.IsNullOrEmpty(testResult.FilePath))
                {
                    var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
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

                _context.TestResults.Remove(testResult);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "تم حذف نتيجة التحليل بنجاح ✓";
            }
            
            return RedirectToAction("DoctorVisits", "Visits", new { date = FilterDate });
        }
    }
}
