using System.Windows.Input;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class CreateSummaryViewModel : DialogViewModelBase
    {
        public CreateSummaryViewModel(PlaceholderOperationResult result, string skippedText)
            : base("Placeholders created")
        {
            CreatedCount = result.Processed;
            SkippedCount = result.Skipped;
            SkippedText = skippedText;
            OpenEditorCommand = new RelayCommand(() => Close(true));
            NotNowCommand = new RelayCommand(() => Close(false));
        }

        public int CreatedCount { get; }

        public int SkippedCount { get; }

        public bool HasSkipped => SkippedCount > 0;

        public string Headline => $"{TextFormat.Quantity(CreatedCount, "placeholder")} created";

        public string SkippedText { get; }

        public bool RememberChoice { get; set; }

        public ICommand OpenEditorCommand { get; }

        public ICommand NotNowCommand { get; }
    }
}
