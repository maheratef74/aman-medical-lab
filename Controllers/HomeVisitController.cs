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

                    // Create directory structure: wwwroot/uploads/{year}/{month}/
                    string year = DateTime.Now.Year.ToString();
                    string month = DateTime.Now.Month.ToString("00");
                    string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", year, month);

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

                    // Construct public URL
                    var request = HttpContext.Request;
                    string baseUrl = $"{request.Scheme}://{request.Host.Value}";
                    imageUrl = $"{baseUrl}/uploads/{year}/{month}/{fileName}";
                }

                return Ok(new { success = true, imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
