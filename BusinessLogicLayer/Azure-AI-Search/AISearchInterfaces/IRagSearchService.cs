using System.Threading.Tasks;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.AISearchInterfaces
{
    public interface IRagSearchService
    {
        Task<RagResponse> GetAnswerAsync(string indexName, string prompt, string? fileUrl = null, int topK = 5);
    }
}
