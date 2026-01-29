using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.TestInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace API_RAG.Controllers
{
    public class HomeController : ControllerBase
    {
        private readonly IFileTextExtractor _fileTextExtractor;
        private readonly ITextExtractorTest _test;
        private readonly IPiiRedactionService _poiRedactionService;
        public HomeController(IFileTextExtractor fileTextExtractor, ITextExtractorTest textExtractor, IPiiRedactionService service)
        {
            _fileTextExtractor = fileTextExtractor;
            _test = textExtractor;
            _poiRedactionService = service;
        }

        [HttpPost("analyze-text")]
        public async Task<IActionResult> AnalyzeText(string text)
        {
            var test = await _poiRedactionService.RedactAsync(text);
            return Ok(test);
        }

        [HttpPost("extract-text")]
        public async Task<IActionResult> ExtractText(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");

            using var stream = file.OpenReadStream();

            var result = await _fileTextExtractor
                .ExtractTextAsync(file.FileName, stream);

            string message = "File processed successfully.";
            if (result.IpDetectedItems != null && result.IpDetectedItems.Count > 0)
            {
                message = "This is intellectual property.";
            }

            return Ok(new
            {
                Message = message,
                FileName = file.FileName,
                PreprocessedText = result.PreprocessedText,
                RedactedText = result.RedactedText,
                SensitiveDataDetected = result.SensitiveDataDetected,
                DetectedTypes = result.DetectedTypes,
                IpDetectedItems = result.IpDetectedItems
            });
        }

        [HttpPost("extract-only-text")]
        public async Task<IActionResult> ExtractOnlyText(IFormFile file)
        {
            if(file == null || file.Length == 0)
                return BadRequest("Invalid file");

            using var stream = file.OpenReadStream();
            var result = await _test
              .TextExtractorTestAsync(file.FileName, stream);
            return Ok(new { message = result });


        }

        [HttpGet("/")]
        public IActionResult Index()
        {
            return Ok("API is running.");
        }
    }
}