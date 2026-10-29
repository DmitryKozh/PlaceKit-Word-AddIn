using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlaceKit.Interfaces;

namespace PlaceKit.Services
{
    // A JSON object of names and values: { "Client name": "Contoso", "Address": "Line 1\nLine 2" }
    public sealed class PlaceholderValuesFile : IPlaceholderValuesFile
    {
        const string FormatError = "It is not a PlaceKit values file (a JSON object of placeholder names and values).";

        public IReadOnlyDictionary<string, string> Read(string path)
        {
            JObject root;
            try
            {
                // DateParseHandling.None: a value like "2026-11-12T10:00" stays the text the user wrote.
                using (var reader = new JsonTextReader(File.OpenText(path)) { DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader);
                }
            }
            catch (JsonReaderException ex)
            {
                throw new InvalidDataException(FormatError, ex);
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (JProperty property in root.Properties())
            {
                switch (property.Value.Type)
                {
                    case JTokenType.Null:
                        break;
                    case JTokenType.String:
                    case JTokenType.Integer:
                    case JTokenType.Float:
                        string value = Convert.ToString(((JValue)property.Value).Value, CultureInfo.InvariantCulture);
                        values[property.Name] = value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                        break;
                    default:
                        throw new InvalidDataException($"The value of \"{property.Name}\" is not text.");
                }
            }

            return values;
        }

        public void Write(string path, IEnumerable<KeyValuePair<string, string>> values)
        {
            var root = new JObject();
            foreach (KeyValuePair<string, string> pair in values)
            {
                root[pair.Key] = pair.Value.Replace("\r\n", "\n");
            }

            File.WriteAllText(path, root.ToString(Formatting.Indented), new UTF8Encoding(false));
        }
    }
}
