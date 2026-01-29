using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.TestInterfaces
{
    public interface ITextExtractorTest
    {
        Task<string> TextExtractorTestAsync(string fileName, Stream fileStream);
    }
}
