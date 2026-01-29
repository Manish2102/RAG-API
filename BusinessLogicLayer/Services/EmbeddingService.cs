using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BusinessLogicLayer.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _deploymentName;

        public EmbeddingService(IConfiguration config)
        {
            _endpoint = config["OpenAI:endpoint"] ?? throw new ArgumentException("");           
            _apiKey = config["OpenAI:apikey"] ?? throw new ArgumentException("");
            _deploymentName = config["OpenAI:deploymentname"] ?? throw new ArgumentException(""); 

            if (string.IsNullOrWhiteSpace(_endpoint))
                throw new ArgumentException("OpenAI:endpoint must be valid URL");
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new ArgumentException("OpenAI:apikey is missing");
            if (string.IsNullOrWhiteSpace(_deploymentName))
                throw new ArgumentException("OpenAI:deploymentname is missing");
        }

        public async Task<float[]> GenerateEmbeddingsAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            try
            {
                using var client = new HttpClient();
                client.BaseAddress = new Uri(_endpoint);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var apiVersion = "2023-05-15";
                var requestUri = $"/openai/deployments/{_deploymentName}/embeddings?api-version={apiVersion}";

                var payload = JsonSerializer.Serialize(new { input = text });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var resp = await client.PostAsync(requestUri, content);
                var respText = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Embedding request failed ({resp.StatusCode}): {respText}");
                    return Array.Empty<float>();
                }

                using var doc = JsonDocument.Parse(respText);
                var root = doc.RootElement;
                if (!root.TryGetProperty("data", out var dataArray) || dataArray.GetArrayLength() == 0)
                    return Array.Empty<float>();

                var embeddingElement = dataArray[0].GetProperty("embedding");
                var list = new float[embeddingElement.GetArrayLength()];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i] = embeddingElement[i].GetSingle();
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Embedding error: {ex.Message}");
                return Array.Empty<float>();
            }
        }
    }
}
