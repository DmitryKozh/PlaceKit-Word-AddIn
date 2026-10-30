using System.Threading;
using System.Threading.Tasks;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IAiClient
    {
        AiProvider Provider { get; }

        Task<string> SendAsync(string apiKey, AiRequest request, CancellationToken token);

        // Lists the models, which costs nothing.
        Task TestKeyAsync(string apiKey, CancellationToken token);
    }
}
