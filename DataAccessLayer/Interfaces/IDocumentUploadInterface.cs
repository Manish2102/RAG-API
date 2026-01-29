using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccessLayer.Interfaces
{
    public interface IDocumentUploadInterface
    {
        Task<string> UploadDocumentAsync(Stream fileStream, string fileName);
        Task<Stream> DownloadDocumentAsync(string fileName);

    }
}
