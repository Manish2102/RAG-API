using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using BusinessLogicLayer.AISearchInterfaces;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.Extensions.Configuration;

public class RagSearchService : IRagSearchService
{
    private readonly IConfiguration _config;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILLmService _llmService;

    public RagSearchService(IConfiguration config, IEmbeddingService embeddingService, ILLmService llmService)
    {
        _config = config;
        _embeddingService = embeddingService;
        _llmService = llmService;
    }

    public async Task<RagResponse> GetAnswerAsync(string indexName, string prompt, string? fileUrl = null, int topK = 5)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingsAsync(prompt);
        if (queryEmbedding == null || queryEmbedding.Length == 0)
            return new RagResponse();

        var endpoint = _config["SearchClient:endpoint"] ?? "";
        var apiKey = _config["SearchClient:apikey"] ?? "";

        var client = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(apiKey));

        var vectorQuery = new VectorizedQuery(queryEmbedding)
        {
            Fields = { "ContentVector" },
            KNearestNeighborsCount = topK
        };

        var options = new SearchOptions
        {
            Size = topK,
            VectorSearch = new()  
            {
                Queries = { vectorQuery }
            }
        };

        var results = new List<SearchIndexModel>();
        var response = await client.SearchAsync<SearchIndexModel>(null, options);

        await foreach (var result in response.Value.GetResultsAsync())
            results.Add(result.Document);

        if (!results.Any())
            return new RagResponse();

        var llmAnswer = await _llmService.GenerateResponseAsync(
            prompt,
            results.Select(r => new RagChunk { Id = r.Id, Content = r.Content })
        );

        return new RagResponse
        {
            Answer = llmAnswer,
            Chunks = results.Select(r => new RagChunk { Id = r.Id, Content = r.Content }).ToList()
        };
    }
}
