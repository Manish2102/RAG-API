using BusinessLogicLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Services
{
    public class TextChunkService : ITextChunkService
    {
        public List<string> ChunkText(string text, int chunkSize = 800, int overlap = 100)
        {
            var chunks = new List<string>();
            int start = 0;

            while (start < text.Length)
            {
                int length = Math.Min(chunkSize, text.Length - start);
                string chunk = text.Substring(start, length);
                chunks.Add(chunk);

                start += chunkSize - overlap;
            }

            return chunks;
        }
    }
}
