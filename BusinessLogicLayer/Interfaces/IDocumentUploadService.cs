using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Interfaces
{
    public interface IDocumentUploadService
    {
        Task<String> UploadDocumentAsync(Stream fileStream, string fileName);
        Task<Stream> DownloadDocumentAsync(string fileName);
    }
}
