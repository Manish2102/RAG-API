using Azure.Messaging;
using BusinessLogicLayer.AISearchInterfaces;
using BusinessLogicLayer.Models;
using Microsoft.AspNetCore.Mvc;

namespace API_RAG.Controllers
{
    [ApiController]
    public class DocumentProcessingAISearch : ControllerBase
    {
        private readonly IDocumentProcessingSearchService _service;
        private readonly IRagSearchService _ragService;
        private readonly IAzureSearchIndexService _indexService;

        public DocumentProcessingAISearch(IDocumentProcessingSearchService service, IRagSearchService ragService, IAzureSearchIndexService indexService)
        {
            _service = service;
            _ragService = ragService;
            _indexService = indexService;
        }

        [HttpPost("create-index")]
        public async Task<IActionResult> Create([FromQuery] string indexName)
        {
            await _indexService.CreateVectorIndexAsync(indexName);
            return Ok(new { message = $"Vector index '{indexName}' created successfully." });
        }

        [HttpPost("file-upload-AI-Search")]
        public async Task<IActionResult> FileUpload(IFormFile file,  [FromQuery] string indexName)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid file");
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest(new { message = "indexName is required" });

            var document = new DocumentFile
            {
                FileName = file.FileName,
                Content = file.OpenReadStream()
            };

            var result = await _service.ProcessDocumentAsync(document, indexName);

            return Ok(result);
        }

        [HttpGet("file-upload-AI-Search/query")]
        public async Task<IActionResult> Query([FromQuery] string indexName, [FromQuery] string q, [FromQuery] string? fileUrl, [FromQuery] int topK = 5)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { Message = "q (query) is required" });
            if (string.IsNullOrWhiteSpace(indexName))
                return BadRequest(new { Message = "indexName is required" });


            var response = await _ragService.GetAnswerAsync(indexName, q, fileUrl, topK);
            return Ok(new { Query = q, Answer = response.Answer, Chunks = response.Chunks });
        }
    }
}
