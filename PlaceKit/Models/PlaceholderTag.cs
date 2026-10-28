using System;

namespace PlaceKit.Models
{
    public static class PlaceholderTag
    {
        public const string Prefix = "PlaceKit-";

        public static string Create(string name) => Prefix + name;

        public static bool TryGetName(string tag, out string name)
        {
            if (tag != null && tag.Length > Prefix.Length && tag.StartsWith(Prefix, StringComparison.Ordinal))
            {
                name = tag.Substring(Prefix.Length);
                return true;
            }

            name = null;
            return false;
        }
    }
}
