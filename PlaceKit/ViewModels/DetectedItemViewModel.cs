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

        public string UsesText => _placeholder.Occurrences == 1 ? "once" : $"{_placeholder.Occurrences} times";

        public string UsesHint => _placeholder.Occurrences == 1
            ? "The text is in the document once."
            : $"The text is in the document {_placeholder.Occurrences} times; each becomes this placeholder.";

        public DetectedPlaceholder ToPlaceholder() => _placeholder.WithName(Name.Trim());
    }
}
