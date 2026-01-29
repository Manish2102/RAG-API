using BusinessLogicLayer.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BusinessLogicLayer.Interfaces
{
    public interface IPiiRedactionService
    {
        Task<PiiRedactionResult> RedactAsync(string text, CancellationToken ct = default);
    }
}
