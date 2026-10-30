using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IAiService
    {
        AiProvider Provider { get; }

        bool HasApiKey { get; }

        string Send(AiRequest request, string progressTitle);

        void TestKey(AiProvider provider, string apiKey);

        // No key yet: offers to open Settings. True once a key is set.
        bool EnsureApiKey(string feature);

        void OpenSettings();
    }
}
