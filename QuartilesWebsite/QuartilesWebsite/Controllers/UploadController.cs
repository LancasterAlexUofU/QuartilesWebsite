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
            string[] allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".tif" };
            string extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Unsupported file type.");
            }

            // Upload folder
            string uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            // File Creation
            string fileName = Path.GetRandomFileName() + extension;
            string filePath = Path.Combine(uploadsDir, fileName);

            using (Stream stream = System.IO.File.Create(filePath))
            {
                await image.CopyToAsync(stream);
            }

            // Character extraction
            using var extractor = new QuartilesOCR(image.FileName);
            var chunks = extractor.ExtractChunksAuto();

            // Delete image after OCR scanning and return list of chunks
            System.IO.File.Delete(filePath);
            return Ok(chunks);
        }
    }
}
