using System.Text.Json.Serialization;

namespace BusinessLogicLayer.Models
{
    public class CosmosVectorModel
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }

        [JsonPropertyName("fileUrl")]
        public required string FileUrl { get; set; }

        [JsonPropertyName("chunkIndex")]
        public int ChunkIndex { get; set; }

        [JsonPropertyName("contentVector")]
        public required float[] ContentVector { get; set; }
    }
}
