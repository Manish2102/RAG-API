using Azure;
using Azure.AI.TextAnalytics;
using BusinessLogicLayer.Configurations;
using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig.DocumentLayoutAnalysis;

namespace BusinessLogicLayer.Services
{
    public class AzurePiiRedactionService : IPiiRedactionService
    {
        private readonly TextAnalyticsClient _textAnalyticsClient;
        //private static readonly HashSet<string> IgnoredCategories =
        //[
        //     "PersonType"
        // ];
        public AzurePiiRedactionService(IOptions<AzureLanguageOptions> options, TextAnalyticsClient client)
        {
            _textAnalyticsClient = client;

        }
        public async Task<PiiRedactionResult> RedactAsync(string text, CancellationToken ct = default)
        {
            // The service will detect all supported PII entities.
            var response = await _textAnalyticsClient
                .RecognizePiiEntitiesAsync(text, cancellationToken: ct);

            var redactedText = text;
            var detectedItems = new List<DetectedPiiItem>();

            var needToDetect = new List<PiiEntityCategory>
            { 
                PiiEntityCategory.INPermanentAccount,
                PiiEntityCategory.INUniqueIdentificationNumber,
            };

            foreach (var entity in response.Value)
            {
                detectedItems.Add(new DetectedPiiItem
                {
                    Type = entity.Category.ToString(),
                    Value = entity.Text
                });
                Console.WriteLine(entity);


                if (!string.Equals(entity.Category.ToString(), "PersonType", StringComparison.OrdinalIgnoreCase))
                {
                    redactedText = redactedText.Replace(
                        entity.Text,
                        $"[REDACTED_{entity.Category}]",
                        StringComparison.OrdinalIgnoreCase
                    );
                }
            }

            return new PiiRedactionResult
            {
                SensitiveDataDetected = detectedItems.Count > 0,
                RedactedText = redactedText,
                DetectedTypes = detectedItems
            };
        }

    }
}