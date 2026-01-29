using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.AISearchInterfaces
{
    public interface IDocumentProcessingSearchService
    {
        Task<object> ProcessDocumentAsync(DocumentFile file, string indexName);
    }
}
