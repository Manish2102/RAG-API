using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BusinessLogicLayer.Interfaces
{
    public interface IFileTextExtractor
    {
        Task<PiiRedactionResult> ExtractTextAsync(string fileName, Stream fileStream);
    }
}
