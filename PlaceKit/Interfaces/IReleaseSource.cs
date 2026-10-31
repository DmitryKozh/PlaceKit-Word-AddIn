using System.Threading;
using System.Threading.Tasks;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IReleaseSource
    {
        // The latest published release. Throws UpdateCheckException.
        Task<ReleaseInfo> GetLatestAsync(CancellationToken token);
    }
}
