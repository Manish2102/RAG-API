using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.Services
{
    public class IpProtectionService : IIpProtectionService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _deploymentName;

        public IpProtectionService(IConfiguration config)
        {
            _endpoint = config["OpenAI:endpoint"] ?? throw new ArgumentException("OpenAI endpoint missing");
            _apiKey = config["OpenAI:apikey"] ?? throw new ArgumentException("OpenAI apikey missing");
            _deploymentName = config["OpenAI:deploymentNameChat"]
                              ?? config["OpenAI:chatdeploymentname"]
                              ?? config["OpenAI:deploymentname"]
                              ?? throw new ArgumentException("OpenAI chat deployment missing");
        }

        public async Task<PiiRedactionResult> ProtectAsync(string text, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new PiiRedactionResult
                {
                    RedactedText = text,
                    SensitiveDataDetected = false,
                    DetectedTypes = new List<DetectedPiiItem>()
                };
            }

            var systemPrompt = """
You are an AI assistant specialized in protecting intellectual property.
Analyze the provided text and identify any intellectual property such as patents, proprietary ideas, algorithms, internal processes, or confidential business knowledge.
Redact the specific details of the identified IP by replacing them with [REDACTED_IP: <Type of IP>].
Return the response strictly as a valid JSON object with the following structure:
{
  "redactedText": "the text with IP redacted",
  "detectedItems": [
    { "type": "the type of IP detected", "value": "the specific text that was redacted" }
  ]
}
If no IP is found, return the original text in redactedText and an empty detectedItems list.
Do not include any markdown formatting (like ```json).
""";

            using var client = new HttpClient();
            client.BaseAddress = new Uri(_endpoint);
            client.DefaultRequestHeaders.Add("api-key", _apiKey);
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));

            var payload = JsonSerializer.Serialize(new
            {
                model = _deploymentName,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                temperature = 1
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var apiVersion = "2024-02-15-preview";

            var resp = await client.PostAsync($"/openai/deployments/{_deploymentName}/chat/completions?api-version={apiVersion}", content, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                Console.WriteLine($"[IP Protection ERROR]: {resp.StatusCode} - {json}");
                // Return original text on error
                return new PiiRedactionResult
                {
                    RedactedText = text,
                    SensitiveDataDetected = false,
                    DetectedTypes = new List<DetectedPiiItem>()
                };
            }

            using var doc = JsonDocument.Parse(json);
            var responseContent = doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString();

            if (responseContent == null) 
            {
                 return new PiiRedactionResult
                {
                    RedactedText = text,
                    SensitiveDataDetected = false,
                    DetectedTypes = new List<DetectedPiiItem>()
                };
            }

            // Clean up markdown if present
            if (responseContent.StartsWith("```json"))
            {
                responseContent = responseContent.Replace("```json", "").Replace("```", "");
            }
            else if (responseContent.StartsWith("```"))
            {
                responseContent = responseContent.Replace("```", "");
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var result = JsonSerializer.Deserialize<IpProtectionResponse>(responseContent, options);

                return new PiiRedactionResult
                {
                    RedactedText = result?.RedactedText ?? text,
                    SensitiveDataDetected = result?.DetectedItems?.Count > 0,
                    DetectedTypes = result?.DetectedItems ?? new List<DetectedPiiItem>()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing IP protection response: {ex.Message}");
                return new PiiRedactionResult
                {
                    RedactedText = text,
                    SensitiveDataDetected = false,
                    DetectedTypes = new List<DetectedPiiItem>()
                };
            }
        }

        private class IpProtectionResponse
        {
            public string RedactedText { get; set; } = string.Empty;
            public List<DetectedPiiItem> DetectedItems { get; set; } = new();
        }
    }
}
