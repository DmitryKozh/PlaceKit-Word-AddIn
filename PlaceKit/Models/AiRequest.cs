using System.Collections.Generic;
using System.Text.Json;

namespace PlaceKit.Models
{
    // The schema is strict: every property required, no additional ones.
    public sealed class AiRequest
    {
        public AiRequest(string instructions, string prompt, IReadOnlyDictionary<string, JsonElement> schema, AiEffort effort)
        {
            Instructions = instructions;
            Prompt = prompt;
            Schema = schema;
            Effort = effort;
        }

        public string Instructions { get; }

        public string Prompt { get; }

        public IReadOnlyDictionary<string, JsonElement> Schema { get; }

        public AiEffort Effort { get; }
    }
}
