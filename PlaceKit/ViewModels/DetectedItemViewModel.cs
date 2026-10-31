using PlaceKit.Models;

namespace PlaceKit.ViewModels
{
    public sealed class DetectedItemViewModel : ViewModelBase
    {
        readonly DetectedPlaceholder _placeholder;

        public DetectedItemViewModel(DetectedPlaceholder placeholder)
        {
            _placeholder = placeholder;
            Name = placeholder.Name;
        }

        public bool IsChosen { get; set; } = true;

        public string Name { get; set; }

        public string Text => _placeholder.Text;

        public string Reason => _placeholder.Reason;

        public bool IsMarker => _placeholder.IsMarker;

        public string UsesText => _placeholder.Occurrences == 1 ? "once" : $"{_placeholder.Occurrences} times";

        public string UsesHint
        {
            get
            {
                string what = IsMarker ? "marker" : "text";
                string becomes = IsMarker ? "this placeholder, empty" : "this placeholder";
                return _placeholder.Occurrences == 1
                    ? $"The {what} is in the document once."
                    : $"The {what} is in the document {_placeholder.Occurrences} times; each becomes {becomes}.";
            }
        }

        public DetectedPlaceholder ToPlaceholder() => _placeholder.WithName(Name.Trim());
    }
}
