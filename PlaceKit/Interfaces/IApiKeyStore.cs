namespace PlaceKit.Interfaces
{
    // The user's Claude API key. It is a secret, so it is kept apart from the settings.
    public interface IApiKeyStore
    {
        bool HasKey { get; }

        // null when no key is saved, or when it was saved by another Windows account.
        string Load();

        void Save(string apiKey);

        void Delete();
    }
}
