using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuartilesToText;

namespace QuartilesWebsite.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image uploaded.");
            }

            // Server side file extension check
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Unsupported file type.");
            }

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");

            var fileName = Path.GetRandomFileName() + extension;
            var filePath = Path.Combine(uploadsDir, fileName);

            using (Stream stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            using var extractor = new QuartilesOCR(image.FileName);
            var chunks = extractor.ExtractChunksAuto();

            return Ok(chunks);
        }
    }
}
