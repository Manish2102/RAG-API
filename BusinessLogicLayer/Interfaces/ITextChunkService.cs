using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Interfaces
{
    public interface ITextChunkService
    {
         List<string> ChunkText(string text, int chunkSize = 1500, int overlap = 200);
    }
}
