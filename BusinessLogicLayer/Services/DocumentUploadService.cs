using BusinessLogicLayer.Interfaces;
using DataAccessLayer.Interfaces;

namespace BusinessLogicLayer.Repositories
{
    public class DocumentUploadService : IDocumentUploadService
    {
        private readonly IDocumentUploadInterface _documentUploadInterface;
        public DocumentUploadService(IDocumentUploadInterface documentUploadInterface)
        {
            _documentUploadInterface = documentUploadInterface;
        }

        public async Task<Stream> DownloadDocumentAsync(string fileName)
        {
            return await _documentUploadInterface.DownloadDocumentAsync(fileName);
        }

        public async Task<string> UploadDocumentAsync(Stream fileStream, string fileName)
        {
            return await _documentUploadInterface.UploadDocumentAsync(fileStream, fileName);
        }
    }
}
