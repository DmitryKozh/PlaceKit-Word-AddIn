namespace PlaceKit.Models
{
    public sealed class PlaceholderInfo
    {
        public PlaceholderInfo(string name, string value, int occurrences, bool hasMixedValues)
        {
            Name = name;
            Value = value;
            Occurrences = occurrences;
            HasMixedValues = hasMixedValues;
        }

        public string Name { get; }

        public string Value { get; }

        public int Occurrences { get; }

        public bool HasMixedValues { get; }
    }
}
