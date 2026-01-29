using BusinessLogicLayer.CosmosInterface;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace API_RAG.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentProcessingCosmosController : ControllerBase
    {
        private readonly IDocumentProcessingCosmosService _service;
        private readonly ICosmosRagServvice _serviceServvice;
        public DocumentProcessingCosmosController(IDocumentProcessingCosmosService service, ICosmosRagServvice ragService)
        {
            _service = service;
            _serviceServvice = ragService;
        }

        [HttpPost("process-and-store")]
        public async Task<IActionResult> ProcessAndStore(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");

            var document = new DocumentFile { FileName = file.FileName, Content = file.OpenReadStream() };
            var result = await _service.ProcessDocumentAsync(document);
            return Ok(result);
        }

        [HttpGet("query")]
        public async Task<IActionResult> Query([FromQuery] string q, [FromQuery] string? fileUrl, [FromQuery] int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { Message = "q (query) is required" });

            var response = await _serviceServvice.GetAnswerAsync(q, fileUrl, topK);
            return Ok(new { Query = q, Answer = response.Answer, Chunks = response.Chunks });
        }
    }
}
