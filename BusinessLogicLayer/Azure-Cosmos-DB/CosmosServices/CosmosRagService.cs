using BusinessLogicLayer.CosmosInterface;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace BusinessLogicLayer.CosmosServices
{
    public class CosmosRagService : ICosmosRagServvice
    {
        private readonly Container _container;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILLmService _llmService;

        public CosmosRagService(
            CosmosClient cosmosClient,
            IConfiguration config,
            IEmbeddingService embeddingService,
            ILLmService llmService)
        {
            _embeddingService = embeddingService;
            _llmService = llmService;

            var dbName = config["cosmos:database"];
            var containerName = config["cosmos:container"];

            _container = cosmosClient.GetContainer(dbName, containerName);
        }

        public async Task<RagResponse> GetAnswerAsync(string prompt, string? fileUrl = null, int topK = 5)
        {
            // Create embedding for the query
            var queryEmbedding = await _embeddingService.GenerateEmbeddingsAsync(prompt);
            if (queryEmbedding == null || queryEmbedding.Length == 0)
            {
                return new RagResponse
                {
                    Answer = "",
                    Chunks = new List<RagChunk>()
                };
            }

            // Correct SQL using correct model property names
            var sql = @"
                SELECT TOP @topK
                    c.id,
                    c.chunkIndex,
                    c.content,
                    c.contentVector
                FROM c
                WHERE (IS_NULL(@fileUrl) OR c.fileUrl = @fileUrl)
                ORDER BY VectorDistance(c.contentVector, @embedding)
            ";

            var query = new QueryDefinition(sql)
                .WithParameter("@topK", topK)
                .WithParameter("@embedding", queryEmbedding)
                .WithParameter("@fileUrl", fileUrl);

            var results = new List<CosmosVectorModel>();

            using var iterator = _container.GetItemQueryIterator<CosmosVectorModel>(query);

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync();
                results.AddRange(page.Resource);
            }

            if (!results.Any())
            {
                return new RagResponse
                {
                    Answer = "No relevant information found.",
                    Chunks = new List<RagChunk>()
                };
            }

            // Fix mapping to match your model
            var chunks = results.Select(x => new RagChunk
            {
                Id = x.Id,
                Content = x.Content
            }).ToList();

            // Generate final LLM answer
            var finalAnswer = await _llmService.GenerateResponseAsync(prompt, chunks);

            return new RagResponse
            {
                Answer = finalAnswer,
                Chunks = chunks
            };
        }
    }
}
