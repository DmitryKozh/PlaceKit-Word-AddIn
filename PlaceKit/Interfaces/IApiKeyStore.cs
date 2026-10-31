using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IApiKeyStore
    {
        bool HasKey(AiProvider provider);

        // null if missing or saved by another Windows account
        string Load(AiProvider provider);

        void Save(AiProvider provider, string apiKey);

        void Delete(AiProvider provider);
    }
}
