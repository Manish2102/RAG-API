using BusinessLogicLayer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogicLayer.CosmosInterface
{
    public interface ICosmosVectorService
    {
        Task StoreDocumentAsync(CosmosVectorModel doc);
        Task<IEnumerable<CosmosVectorModel>> GetByFileUrlAsync(string fileUrl);
    }
}
