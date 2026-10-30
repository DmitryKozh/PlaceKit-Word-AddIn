using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    // The user's API keys, one per service. They are secrets, so they are kept apart from the settings.
    public interface IApiKeyStore
    {
        bool HasKey(AiProvider provider);

        // null when no key is saved, or when it was saved by another Windows account.
        string Load(AiProvider provider);

        void Save(AiProvider provider, string apiKey);

        void Delete(AiProvider provider);
    }
}
