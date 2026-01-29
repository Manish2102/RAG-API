using System.Collections.Generic;

namespace BusinessLogicLayer.Models
{
    public class RagResponse
    {
        public string Answer { get; set; } = string.Empty;
        public List<RagChunk> Chunks { get; set; } = new List<RagChunk>();
    }

    public class RagChunk
    {
        public string Id { get; set; }
        public string Content { get; set; }
    }
}
