using BusinessLogicLayer.CosmosInterface;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services
{
    public class DocumentProcessingCosmosService : IDocumentProcessingCosmosService
    {
        private readonly IDocumentUploadService _documentUploadService;
        private readonly IFileTextExtractor _fileTextExtractor;
        private readonly ITextChunkService _textChunkService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ICosmosVectorService _cosmosVectorService;

        public DocumentProcessingCosmosService(
            IDocumentUploadService documentUploadService,
            IFileTextExtractor fileTextExtractor,
            ITextChunkService textChunkService,
            IEmbeddingService embeddingService,
            ICosmosVectorService cosmosVectorService)
        {
            _documentUploadService = documentUploadService;
            _fileTextExtractor = fileTextExtractor;
            _textChunkService = textChunkService;
            _embeddingService = embeddingService;
            _cosmosVectorService = cosmosVectorService;
        }

        public async Task<object> ProcessDocumentAsync(DocumentFile file)
        {
            // Upload document ? returns blob URL
            var fileUrl = await _documentUploadService.UploadDocumentAsync(file.Content, file.FileName);

            // Download again for text extraction
            var blobStream = await _documentUploadService.DownloadDocumentAsync(file.FileName);

            // Extract text
            var extractedText = await _fileTextExtractor.ExtractTextAsync(file.FileName, blobStream);

            // Split into chunks
            var chunks = _textChunkService.ChunkText(extractedText.RedactedText);

            int storedCount = 0;
            int index = 0;

            foreach (var chunk in chunks)
            {
                // Generate embedding for each chunk
                var embedding = await _embeddingService.GenerateEmbeddingsAsync(chunk);

                // Match EXACTLY your model property names:
                var cosmosDoc = new CosmosVectorModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = chunk,
                    FileUrl = fileUrl,
                    ChunkIndex = index,
                    ContentVector = embedding
                };

                await _cosmosVectorService.StoreDocumentAsync(cosmosDoc);

                storedCount++;
                index++;
            }

            return new
            {
                Message = "Uploaded and stored in Cosmos successfully.",
                FileName = file.FileName,
                FileUrl = fileUrl,
                TotalChunks = storedCount
            };
        }
    }
}
