using BusinessLogicLayer.CosmosInterface;
using BusinessLogicLayer.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.CosmosServices
{
    public class CosmosVectorService : ICosmosVectorService
    {
        private readonly CosmosClient _client;
        private readonly Container _container;

        public CosmosVectorService(IConfiguration config)
        {
            var connectionString = config["cosmos:connectionstring"];
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("cosmos:connectionstring missing");

            var database = config["cosmos:database"];
            if (string.IsNullOrWhiteSpace(database))
                throw new ArgumentException("cosmos:database missing");

            var container = config["cosmos:container"];
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentException("cosmos:container missing");

            _client = new CosmosClient(connectionString);
            _container = _client.GetContainer(database, container);
        }

        public async Task StoreDocumentAsync(CosmosVectorModel doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            await _container.UpsertItemAsync(doc, new PartitionKey(doc.Id));
        }

        public async Task<IEnumerable<CosmosVectorModel>> GetByFileUrlAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException(nameof(fileUrl));

            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.fileUrl = @fileUrl"
            )
            .WithParameter("@fileUrl", fileUrl);

            var results = new List<CosmosVectorModel>();

            using var iterator = _container.GetItemQueryIterator<CosmosVectorModel>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }
    }
}
