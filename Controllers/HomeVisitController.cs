using Microsoft.AspNetCore.Mvc;

namespace DrMohamedWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeVisitController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public HomeVisitController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("UploadRequest")]
        public async Task<IActionResult> UploadRequest(IFormFile? image)
        {
            try
            {
                string imageUrl = "";

                if (image != null && image.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { success = false, message = "Only JPG and PNG images are allowed." });
                    }

                    // Use ContentRootPath to avoid double wwwroot issue
                    string year = DateTime.Now.Year.ToString();
                    string month = DateTime.Now.Month.ToString("00");
                    string uploadFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", year, month);

                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // Generate unique filename
                    string fileName = Guid.NewGuid().ToString("N") + extension;
                    string filePath = Path.Combine(uploadFolder, fileName);

                    // Save file
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    // Return relative URL that maps to our GetImage action
                    imageUrl = $"/api/HomeVisit/Image/{year}/{month}/{fileName}";
                }

                return Ok(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Image/{year}/{month}/{fileName}")]
        public IActionResult GetImage(string year, string month, string fileName)
        {
            // Sanitize inputs
            if (string.IsNullOrWhiteSpace(year) || string.IsNullOrWhiteSpace(month) || string.IsNullOrWhiteSpace(fileName))
                return NotFound();

            string filePath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", year, month, fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            string contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType);
        }
    }
}
