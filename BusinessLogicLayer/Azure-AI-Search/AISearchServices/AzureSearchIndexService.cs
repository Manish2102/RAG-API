using Azure;
using Azure.Search.Documents;
using BusinessLogicLayer.Models;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

using Microsoft.Extensions.Configuration;
using BusinessLogicLayer.AISearchInterfaces;

namespace BusinessLogicLayer.AISearchServices
{
    public class AzureSearchIndexService : IAzureSearchIndexService
    {
        private readonly SearchIndexClient _client;
        private readonly string _endpoint;
        private readonly string _apiKey;
        public AzureSearchIndexService(IConfiguration config, SearchIndexClient client)
        {
            _endpoint = config["SearchClient:endpoint"] ?? throw new ArgumentException("Azure Search endpoint required");
            _apiKey = config["SearchClient:apikey"] ?? throw new ArgumentException("api key is required");
            _client = client;
        }

        public async Task CreateVectorIndexAsync(string indexName)
        {
            var index = new SearchIndex(indexName)
            {
                Fields = new FieldBuilder().Build(typeof(SearchIndexModel)),

                VectorSearch = new VectorSearch()
                {
                    Algorithms =
                {
                    new HnswAlgorithmConfiguration("my-hnsw")
                },
                    Profiles =
                {
                    new VectorSearchProfile(
                        name: "my-vector-profile",
                        algorithmConfigurationName: "my-hnsw")
                }
                }
            };

            var vectorField = index.Fields.First(f => f.Name == nameof(SearchIndexModel.ContentVector));
            vectorField.VectorSearchDimensions = 1536; 
            vectorField.VectorSearchProfileName = "my-vector-profile";

            await _client.CreateOrUpdateIndexAsync(index);
        }

        public async Task SendToSearchAsync(string indexName, SearchIndexModel doc)
        {
            var searchClient = new SearchClient(new Uri(_endpoint), indexName, new AzureKeyCredential(_apiKey));
            await searchClient.MergeOrUploadDocumentsAsync(new[] { doc });
        }
    }
}
