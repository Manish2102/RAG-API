using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Text;
using UglyToad.PdfPig;

namespace BusinessLogicLayer.Repositories
{
    public class FileTextExtracterService : IFileTextExtractor
    {
        private readonly IPiiRedactionService _piiService;
        private readonly IIpProtectionService _ipService;

        public FileTextExtracterService(IPiiRedactionService piiService, IIpProtectionService ipService)
        {
            _piiService = piiService;
            _ipService = ipService;
        }

        public async Task<PiiRedactionResult> ExtractTextAsync(string fileName, Stream fileStream)
        {
            fileStream.Position = 0;
            var extension = Path.GetExtension(fileName).ToLower();

            string extractedText = extension switch
            {
                ".pdf" => ExtractPdf(fileStream),
                ".docx" => ExtractDocx(fileStream),
                ".xlsx" => ExtractXlsx(fileStream),
                ".pptx" => ExtractPptx(fileStream),
                ".txt" or ".log" or ".json" or ".xml" or ".csv" or ".md" => await ExtractTxtAsync(fileStream),
                _ => throw new NotSupportedException($"Unsupported file type: {extension}")
            };
            var preprocessText = PreprocessText(extractedText);
            
            // 1. PII Redaction
            var piiResult = await _piiService.RedactAsync(preprocessText);

            return new PiiRedactionResult
            {
                PreprocessedText = preprocessText,
                RedactedText = piiResult.RedactedText,
                SensitiveDataDetected = piiResult.SensitiveDataDetected,
                DetectedTypes = (IReadOnlyList<DetectedPiiItem>)piiResult.DetectedTypes,
                //IpDetectedItems = new List<DetectedPiiItem>() // No IP detection
            };
        }

        private async Task<string> ExtractTxtAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private string ExtractPdf(Stream stream)
        {
            var sb = new StringBuilder();
            try 
            {
                using var pdf = PdfDocument.Open(stream);
                foreach (var page in pdf.GetPages())
                    sb.AppendLine(page.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting PDF: {ex.Message}");
            }
            return sb.ToString();
        }

        private string ExtractDocx(Stream stream)
        {
            var sb = new StringBuilder();
            try
            {
                using var doc = WordprocessingDocument.Open(stream, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    sb.Append(body.InnerText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting DOCX: {ex.Message}");
                // Fallback or rethrow depending on requirement. 
                // For now, return empty string or partial content if failed.
            }
            return sb.ToString();
        }

        private string ExtractXlsx(Stream stream)
        {
            var sb = new StringBuilder();
            using var doc = SpreadsheetDocument.Open(stream, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return string.Empty;

            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                using var reader = OpenXmlReader.Create(worksheetPart);
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Cell))
                    {
                        var cell = (Cell)reader.LoadCurrentElement();
                        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                        {
                            if (int.TryParse(cell.InnerText, out int id) && sharedStringTable != null)
                            {
                                var item = sharedStringTable.ElementAtOrDefault(id);
                                if (item != null) sb.Append(item.InnerText + " ");
                            }
                        }
                        else if (cell.InnerText != null)
                        {
                            sb.Append(cell.InnerText + " ");
                        }
                    }
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private string ExtractPptx(Stream stream)
        {
            var sb = new StringBuilder();
            using var doc = PresentationDocument.Open(stream, false);
            var presentationPart = doc.PresentationPart;
            if (presentationPart != null)
            {
                foreach (var slidePart in presentationPart.SlideParts)
                {
                    if (slidePart.Slide != null)
                    {
                        // Iterate through all text elements in the slide
                        // Note: Text is typically in Shape -> TextBody -> Paragraph -> Run -> Text
                        // We use Descendants to find all Text elements from DocumentFormat.OpenXml.Drawing
                        foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        {
                             sb.Append(text.Text + " ");
                        }
                        sb.AppendLine();
                    }
                }
            }
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
