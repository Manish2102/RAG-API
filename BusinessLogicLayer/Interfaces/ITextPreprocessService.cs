using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Interfaces
{
    public interface ITextPreprocessService
    {
        Task<string> PreProcessText(string text);
    }
}
