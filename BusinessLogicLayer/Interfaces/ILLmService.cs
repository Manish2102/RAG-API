using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface ILLmService
    {
        Task<string> GenerateResponseAsync(string query, IEnumerable<RagChunk> documents, double temperature = 0.7, int maxTokens = 1024);
    }
}
