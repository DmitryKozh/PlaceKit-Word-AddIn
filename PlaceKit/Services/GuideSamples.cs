using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.Services
{
    // In the guide, Detect and From text work without an API key: a sample answer, written for the guide, stands in for
    // the AI service's and goes through the same checks against the document. With a key, the real service answers.
    internal static class GuideSamples
    {
        // Marks the guide document. Once saved, it is the user's own document, maybe a template, and no longer the guide.
        public const string DocumentVariable = "PlaceKitGuide";

        // The guide's last page has no markers, for Detect: its heading, the company, then the rest of the letter.
        public static readonly string[] OrderConfirmation =
        {
            "Order confirmation 4711",
            "Contoso Ltd",
            "12 Harbour Road, Hamburg",
            "Dear Anna Berg,",
            "Thank you for your order 4711 of 12 November 2026. We will deliver the coffee machines X200 on 20 November 2026.",
            "The total is EUR 1,180.00, payable within 30 days.",
            "Kind regards,",
            "Northwind Traders",
        };

        // Filled in for From text. Each value below is found by the words around it here.
        public const string Email =
            "Hamburg, 19 November 2026\r\n\r\n" +
            "Hello Tom Weber,\r\n\r\n" +
            "On 3 November 2026 we received order 4711 from Northwind Traders. The Coffee machine X200 (quantity: 2) does not heat up. " +
            "Please send a replacement and quote claim WC-118.\r\n\r\n" +
            "Anna Berg\r\n" +
            "Contoso Ltd, 12 Harbour Road, Hamburg";

        // The sample answer for the order confirmation. Northwind Traders is left out: the letter is from them.
        static readonly (string Name, string Text, string Reason)[] Suggestions =
        {
            ("Client", "Contoso Ltd", "The customer's company"),
            ("Client address", "12 Harbour Road, Hamburg", "The customer's address"),
            ("Contact", "Anna Berg", "The person the letter is addressed to"),
            ("Confirmation number", "4711", "The order number, in the heading and the text"),
            ("Order date", "12 November 2026", "When the order was placed"),
            ("Product", "coffee machines X200", "What was ordered"),
            ("Delivery day", "20 November 2026", "When it will be delivered"),
            ("Total", "EUR 1,180.00", "The amount due"),
        };

        // Evidence: what the text must say for the value to count, so "2" isn't found in any date.
        static readonly (string Name, string Value, string Evidence)[] Values =
        {
            ("Customer", "Contoso Ltd", "Contoso Ltd"),
            ("Customer address", "12 Harbour Road, Hamburg", "12 Harbour Road, Hamburg"),
            ("Date", "19 November 2026", "Hamburg, 19 November 2026"),
            ("Supplier", "Northwind Traders", "from Northwind Traders"),
            ("Supplier contact", "Tom Weber", "Hello Tom Weber"),
            ("Claim number", "WC-118", "claim WC-118"),
            ("Delivery date", "3 November 2026", "On 3 November 2026 we received"),
            ("Order number", "4711", "order 4711"),
            ("Item", "Coffee machine X200", "Coffee machine X200"),
            ("Quantity", "2", "quantity: 2"),
            ("Problem", "Does not heat up", "does not heat up"),
            ("Customer contact", "Anna Berg", "Anna Berg"),
        };

        public static IPlaceholderDetector Detector { get; } = new SampleDetector();

        public static IValueFinder ValueFinder { get; } = new SampleValueFinder();

        // The rule for both features: the guide, and no key to ask the real service with.
        public static bool Apply(bool isGuide, IAiService ai) => isGuide && !ai.HasApiKey;

        sealed class SampleDetector : IPlaceholderDetector
        {
            // Only what is in the part Detect reads, such as the selection.
            public IReadOnlyList<DetectedPlaceholder> Detect(string text, string documentText, string note, IReadOnlyList<string> existingNames, PlaceholderPattern markers) =>
                DetectRequest.Parse(Answer(), documentText, existingNames)
                    .Where(placeholder => WordText.CountWholeWords(text, placeholder.Text) > 0)
                    .ToList();

            static string Answer() => JsonSerializer.Serialize(new
            {
                placeholders = Suggestions.Select(suggestion => new { name = suggestion.Name, text = suggestion.Text, reason = suggestion.Reason }),
            });
        }

        // Answers with the values the text still holds, so an edited email gets fewer.
        sealed class SampleValueFinder : IValueFinder
        {
            public IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names)
            {
                HashSet<string> asked = new HashSet<string>(names, StringComparer.Ordinal);
                Dictionary<string, string> found = Values
                    .Where(value => asked.Contains(value.Name) && text.IndexOf(value.Evidence, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
                if (found.Count == 0)
                {
                    throw new AiServiceException("Without an API key, From text in the guide works on the sample email only. Set up AI to find the values in any text.");
                }

                return found;
            }
        }
    }
}
