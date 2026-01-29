using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.AISearchInterfaces
{
    public interface IAzureSearchIndexService
    {
        Task CreateVectorIndexAsync(string indexName);
        Task SendToSearchAsync(string indexName, SearchIndexModel doc);
    }
}
