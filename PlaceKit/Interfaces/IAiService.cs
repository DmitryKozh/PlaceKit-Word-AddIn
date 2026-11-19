using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IAiService
    {
        AiProvider Provider { get; }

        bool HasApiKey { get; }

        string Send(AiRequest request, string progressTitle);

        void TestKey(AiProvider provider, string apiKey);

        // No key yet: shows the AI setup. True once there is a key.
        bool EnsureApiKey(string feature);

        // The AI setup, asked for by a feature such as "Detect", or null. True if there is a key afterwards.
        bool SetUp(string feature);

        void OpenSettings();
    }
}
