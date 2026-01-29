using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.TestInterfaces;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using UglyToad.PdfPig;

namespace BusinessLogicLayer.TestServices
{
    public class TextExtractorService : ITextExtractorTest
    {
        private readonly ITextPreprocessService _preProcess;
        private readonly IPiiRedactionService _redactionService;
        private readonly IIpProtectionService _ipService;

        public TextExtractorService(ITextPreprocessService preProcess, IPiiRedactionService redactionService, IIpProtectionService ipService)
        {
            _preProcess = preProcess;
            _redactionService = redactionService;
            _ipService = ipService;
        }

        public async Task<string> TextExtractorTestAsync(string fileName, Stream fileStream)
        {
            fileStream.Position = 0;
            var extension = Path.GetExtension(fileName).ToLower();

            string extractedText = extension switch
            {
                ".pdf" => ExtractPdf(fileStream),
                ".docx" => ExtractDocx(fileStream),
                ".txt" => await ExtractTxtAsync(fileStream),
                _ => throw new NotSupportedException($"Unsupported file type: {extension}")
            };

            var preProcessedText = PreprocessText(extractedText);
            var redactionResult = await _redactionService.RedactAsync(preProcessedText);
            
            var ipResult = await _ipService.ProtectAsync(redactionResult.RedactedText);

            // Fix: Return the redacted text property from the result
            return ipResult.RedactedText;
        }

        private async Task<string> ExtractTxtAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private string ExtractPdf(Stream stream)
        {
            var sb = new StringBuilder();
            using var pdf = PdfDocument.Open(stream);
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private string ExtractDocx(Stream stream)
        {
            var sb = new StringBuilder();
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart.Document.Body;
            sb.Append(body.InnerText);
            return sb.ToString();
        }

        private string PreprocessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // Remove punctuation
            var cleaned = new string(text.Where(c => !char.IsPunctuation(c)).ToArray());

            // Normalize whitespace
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");

            return cleaned.Trim();
        }
    }
}
