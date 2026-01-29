using BusinessLogicLayer.AISearchInterfaces;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.AISearchServices
{
    public class DocumentProcessingService : IDocumentProcessingSearchService
    {
        private readonly IDocumentUploadService _documentUploadService;
        private readonly IFileTextExtractor _fileTextExtractor;
        private readonly ITextChunkService _textChunkService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IAzureSearchIndexService _azureSearchIndexService;
        public DocumentProcessingService(
            IDocumentUploadService documentUploadService,
            IFileTextExtractor fileTextExtractor,
            ITextChunkService textChunkService,
            IEmbeddingService embeddingService,
            IAzureSearchIndexService azureSearchIndexService)
        {
            _documentUploadService = documentUploadService;
            _fileTextExtractor = fileTextExtractor;
            _textChunkService = textChunkService;
            _embeddingService = embeddingService;
            _azureSearchIndexService = azureSearchIndexService;
        }
        public async Task<object> ProcessDocumentAsync(DocumentFile file, string indexName)
        {
            var fileUrl = await _documentUploadService.UploadDocumentAsync(file.Content, file.FileName);

            var blobStream = await _documentUploadService.DownloadDocumentAsync(file.FileName);

            var extractedText = await _fileTextExtractor.ExtractTextAsync(file.FileName, blobStream);

            Console.WriteLine($"Extracted Text: {extractedText}");

            var chunks = _textChunkService.ChunkText(extractedText.RedactedText);

            int storedCount = 0;
            int index = 0;


            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.GenerateEmbeddingsAsync(chunk);

                var searchDoc = new SearchIndexModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = chunk,               
                    ContentVector = embedding       
                };

                await _azureSearchIndexService.SendToSearchAsync(indexName, searchDoc);
                storedCount++;
                index++;
            }

            return new
            {
                Message = "Uploaded successfully.",
                FileName = file.FileName,
                FileUrl = fileUrl,
                TotalChunks = storedCount
            };

        }
    }
}
