using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.CosmosInterface
{
    public interface ICosmosRagServvice
    {
        Task<RagResponse> GetAnswerAsync(string prompt, string? fileUrl = null, int topK = 5);
    }
}
