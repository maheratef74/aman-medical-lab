using DrMohamedWeb.Core.Entities;
using DrMohamedWeb.Infrastructure.Data;
using DrMohamedWeb.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DrMohamedWeb.Controllers
{
    public class ResultController : Controller
    {
        private readonly AmanDbContext _context;

        public ResultController(AmanDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                ModelState.AddModelError("", "الرجاء إدخال رقم الهاتف");
                return View("Search");
            }

            var patient = await _context.Patients
                .Include(p => p.Visits.Where(v => v.IsAvailable))
                .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone);

            if (patient == null || !patient.Visits.Any())
            {
                TempData["ErrorMessage"] = "لا يوجد نتائج لهذا الرقم";
                return RedirectToAction(nameof(Search));
            }

            var latestVisit = patient.Visits.OrderByDescending(v => v.VisitDate).FirstOrDefault();

            if (latestVisit == null)
            {
                TempData["ErrorMessage"] = "لا توجد زيارات متاحة لهذا المريض";
                return RedirectToAction(nameof(Search));
            }

            var viewModel = new PatientResultViewModel
            {
                PatientName = patient.Name,
                PhoneNumber = patient.PhoneNumber,
                LatestVisit = new VisitViewModel
                {
                    Id = latestVisit.Id,
                    VisitDate = latestVisit.VisitDate,
                    Notes = latestVisit.Notes,
                    TestResults = latestVisit.TestResults.Select(tr => new TestResultViewModel
                    {
                        Id = tr.Id,
                        TestName = tr.TestName,
                        FilePath = tr.FilePath
                    }).ToList()
                }
            };

            return View("LatestResult", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SearchByPhoneAjax(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest("<div class='alert alert-danger text-center'>الرجاء إدخال رقم الهاتف</div>");
            }

            var patient = await _context.Patients
                .Include(p => p.Visits.Where(v => v.IsAvailable))
                .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone);

            if (patient == null || !patient.Visits.Any())
            {
                return NotFound("<div class='alert alert-danger text-center'>لا يوجد نتائج لهذا الرقم</div>");
            }

            var latestVisit = patient.Visits.OrderByDescending(v => v.VisitDate).FirstOrDefault();

            if (latestVisit == null)
            {
                return NotFound("<div class='alert alert-danger text-center'>لا توجد زيارات متاحة لهذا المريض</div>");
            }

            var viewModel = new PatientResultViewModel
            {
                PatientName = patient.Name,
                PhoneNumber = patient.PhoneNumber,
                LatestVisit = new VisitViewModel
                {
                    Id = latestVisit.Id,
                    VisitDate = latestVisit.VisitDate,
                    Notes = latestVisit.Notes,
                    TestResults = latestVisit.TestResults.Select(tr => new TestResultViewModel
                    {
                        Id = tr.Id,
                        TestName = tr.TestName,
                        FilePath = tr.FilePath
                    }).ToList()
                }
            };

            return PartialView("LatestResultPartial", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVisits(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return RedirectToAction(nameof(Search));
            }

            var patient = await _context.Patients
                .Include(p => p.Visits.Where(v => v.IsAvailable))
                .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone);

            if (patient == null || !patient.Visits.Any())
            {
                TempData["ErrorMessage"] = "لا توجد زيارات لهذا المريض";
                return RedirectToAction(nameof(Search));
            }

            var viewModel = new PatientResultViewModel
            {
                PatientName = patient.Name,
                PhoneNumber = patient.PhoneNumber,
                AllVisits = patient.Visits.OrderByDescending(v => v.VisitDate).Select(v => new VisitViewModel
                {
                    Id = v.Id,
                    VisitDate = v.VisitDate,
                    Notes = v.Notes,
                    TestResults = v.TestResults.Select(tr => new TestResultViewModel
                    {
                        Id = tr.Id,
                        TestName = tr.TestName,
                        FilePath = tr.FilePath
                    }).ToList()
                }).ToList()
            };

            return View("AllVisits", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GetAllVisitsAjax(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return BadRequest("<div class='alert alert-danger text-center'>الرجاء إدخال رقم الهاتف</div>");
            }

            var patient = await _context.Patients
                .Include(p => p.Visits.Where(v => v.IsAvailable))
                .ThenInclude(v => v.TestResults)
                .FirstOrDefaultAsync(p => p.PhoneNumber == phone);

            if (patient == null || !patient.Visits.Any())
            {
                return NotFound("<div class='alert alert-danger text-center'>لا توجد زيارات لهذا المريض</div>");
            }

            var viewModel = new PatientResultViewModel
            {
                PatientName = patient.Name,
                PhoneNumber = patient.PhoneNumber,
                AllVisits = patient.Visits.OrderByDescending(v => v.VisitDate).Select(v => new VisitViewModel
                {
                    Id = v.Id,
                    VisitDate = v.VisitDate,
                    Notes = v.Notes,
                    TestResults = v.TestResults.Select(tr => new TestResultViewModel
                    {
                        Id = tr.Id,
                        TestName = tr.TestName,
                        FilePath = tr.FilePath
                    }).ToList()
                }).ToList()
            };

            return PartialView("AllVisitsPartial", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var testResult = await _context.TestResults.FindAsync(id);
            if (testResult == null || string.IsNullOrWhiteSpace(testResult.FilePath))
            {
                return NotFound();
            }

            // Map the relative path to physical path 
            // In a real application, make sure this maps securely to your storage location
            var filepath = System.IO.Path.Combine(
                           System.IO.Directory.GetCurrentDirectory(),
                           "wwwroot", testResult.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filepath))
            {
                return NotFound();
            }

            var fileName = System.IO.Path.GetFileName(filepath);
            return PhysicalFile(filepath, "application/pdf", fileName);
        }
    }
}
