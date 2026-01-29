using BusinessLogicLayer.Models;
using System.Threading;
using System.Threading.Tasks;

namespace BusinessLogicLayer.Interfaces
{
    public interface IIpProtectionService
    {
        Task<PiiRedactionResult> ProtectAsync(string text, CancellationToken ct = default);
    }
}
