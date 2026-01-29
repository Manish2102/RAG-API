//using Azure;
//using Azure.AI.TextAnalytics;
//using BusinessLogicLayer.Configurations;
//using BusinessLogicLayer.Interfaces;
//using BusinessLogicLayer.Models;
//using Microsoft.Extensions.Options;
//using System.Text;
//using System.Text.RegularExpressions;

//namespace BusinessLogicLayer.Services
//{
//    public class TestPiiRedactService : IPiiRedactionService
//    {
//        private readonly TextAnalyticsClient _textAnalyticsClient;

//        public TestPiiRedactService(IOptions<AzureLanguageOptions> options)
//        {
//            var config = options.Value;

//            var clientOptions = new TextAnalyticsClientOptions
//            {
//                Diagnostics =
//                {
//                    IsLoggingContentEnabled = false
//                }
//            };

//            _textAnalyticsClient = new TextAnalyticsClient(
//                new Uri(config.endpoint),
//                new AzureKeyCredential(config.apikey),
//                clientOptions);
//        }

//        public async Task<PiiRedactionResult> RedactAsync(
//            string text,
//            CancellationToken ct = default)
//        {
//            var detectedItems = new List<DetectedPiiItem>();
//            var redactedBuilder = new StringBuilder();

//            var piiOptions = new RecognizePiiEntitiesOptions
//            {
//                DomainFilter = PiiEntityDomain.None
//            };

//            foreach (var chunk in SplitIntoChunks(text))
//            {
//                var redactedChunk = chunk;

//                var response = await _textAnalyticsClient
//                    .RecognizePiiEntitiesAsync(
//                        chunk,
//                        language: "en",
//                        piiOptions,
//                        ct);

//                foreach (var entity in response.Value)
//                {
//                    detectedItems.Add(new DetectedPiiItem
//                    {
//                        Type = entity.Category.ToString(),
//                        Value = entity.Text
//                    });

//                    redactedChunk = SafeReplace(
//                        redactedChunk,
//                        entity.Text,
//                        $"[REDACTED_{entity.Category}]");
//                }

//                redactedBuilder.Append(redactedChunk);
//            }

//            return new PiiRedactionResult
//            {
//                RedactedText = redactedBuilder.ToString(),
//                SensitiveDataDetected = detectedItems.Any(),
//                DetectedTypes = detectedItems
//                    .GroupBy(x => $"{x.Type}:{x.Value}")
//                    .Select(g => g.First())
//                    .ToList()
//            };
//        }

//        // 🔹 Chunking (Azure limit safe)
//        private static IEnumerable<string> SplitIntoChunks(
//            string text,
//            int chunkSize = 4500)
//        {
//            for (int i = 0; i < text.Length; i += chunkSize)
//            {
//                yield return text.Substring(
//                    i,
//                    Math.Min(chunkSize, text.Length - i));
//            }
//        }

//        // 🔹 Safe replacement
//        private static string SafeReplace(
//            string input,
//            string value,
//            string replacement)
//        {
//            return Regex.Replace(
//                input,
//                Regex.Escape(value),
//                replacement,
//                RegexOptions.IgnoreCase);
//        }
//    }
//}
