using BusinessLogicLayer.Interfaces;
using BusinessLogicLayer.Models;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;

namespace BusinessLogicLayer.Services
{
    public class LLmService : ILLmService
    {
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _deploymentName;

        public LLmService(IConfiguration config)
        {
            _endpoint = config["OpenAI:endpoint"] ?? throw new ArgumentException("OpenAI endpoint missing");
            _apiKey = config["OpenAI:apikey"] ?? throw new ArgumentException("OpenAI apikey missing");
            _deploymentName = config["OpenAI:deploymentNameChat"]
                              ?? config["OpenAI:chatdeploymentname"]
                              ?? config["OpenAI:deploymentname"]
                              ?? throw new ArgumentException("OpenAI chat deployment missing");
        }

        public async Task<string> GenerateResponseAsync(string userQuery, IEnumerable<RagChunk> docs, double temperature = 0.5, int maxTokens = 1024)
        {
            var context = BuildContext(docs);

            var systemPrompt = """
You are an AI assistant that answers based ONLY on provided context.
If the answer is not in the context, respond only with "I don't know".
Do not hallucinate. Do not answer from general knowledge.
""";

            var requestMessage = $"""
Question: {userQuery}

Context:
{context}

Answer strictly based on the context:
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
                    new { role = "user", content = requestMessage }
                }
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var apiVersion = "2024-02-15-preview";

            var resp = await client.PostAsync($"/openai/deployments/{_deploymentName}/chat/completions?api-version={apiVersion}", content);
            var json = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return $"[LLM ERROR]: {resp.StatusCode} - {json}";
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString();
        }

        private string BuildContext(IEnumerable<RagChunk> docs)
        {
            return string.Join("\n\n---\n\n", docs.Select(d => d.Content));
        }
    }
}
