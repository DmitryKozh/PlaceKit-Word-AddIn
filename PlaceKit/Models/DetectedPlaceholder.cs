namespace PlaceKit.Models
{
    public sealed class DetectedPlaceholder
    {
        public DetectedPlaceholder(string name, string text, string reason, int occurrences, bool isMarker = false)
        {
            Name = name;
            Text = text;
            Reason = reason;
            Occurrences = occurrences;
            IsMarker = isMarker;
        }

        public string Name { get; }

        public string Text { get; }

        public string Reason { get; }

        // whole-word matches in the document
        public int Occurrences { get; }

        // A marker in the user's style, such as {{Customer}}: it becomes an empty placeholder, as Create makes it.
        public bool IsMarker { get; }

        public DetectedPlaceholder WithName(string name) => new DetectedPlaceholder(name, Text, Reason, Occurrences, IsMarker);
    }
}
