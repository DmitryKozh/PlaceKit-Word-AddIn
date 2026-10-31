namespace PlaceKit.Models
{
    public sealed class DetectedPlaceholder
    {
        public DetectedPlaceholder(string name, string text, string reason, int occurrences)
        {
            Name = name;
            Text = text;
            Reason = reason;
            Occurrences = occurrences;
        }

        public string Name { get; }

        public string Text { get; }

        public string Reason { get; }

        // whole-word matches in the document
        public int Occurrences { get; }

        public DetectedPlaceholder WithName(string name) => new DetectedPlaceholder(name, Text, Reason, Occurrences);
    }
}
