using System;

namespace PlaceKit.Models
{
    public static class AiProviders
    {
        public static readonly AiProvider[] All = { AiProvider.Claude, AiProvider.OpenAI };

        public static string Name(AiProvider provider) => provider == AiProvider.OpenAI ? "ChatGPT" : "Claude";

        public static string Company(AiProvider provider) => provider == AiProvider.OpenAI ? "OpenAI" : "Anthropic";

        public static string KeyName(AiProvider provider) => provider == AiProvider.OpenAI ? "OpenAI API key" : "Claude API key";

        public static Uri KeysPage(AiProvider provider) => provider == AiProvider.OpenAI
            ? new Uri("https://platform.openai.com/api-keys")
            : new Uri("https://console.anthropic.com/settings/keys");

        public static string KeysPageLink(AiProvider provider) => provider == AiProvider.OpenAI ? "Get a key on the OpenAI platform" : "Get a key in the Claude Console";
    }
}
