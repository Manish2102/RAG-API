using BusinessLogicLayer.Models;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDocumentProcessingCosmosService
    {
        Task<object> ProcessDocumentAsync(DocumentFile file);
    }
}
