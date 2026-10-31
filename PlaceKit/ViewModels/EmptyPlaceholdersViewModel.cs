using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PlaceKit.Models;
using PlaceKit.Tools;

namespace PlaceKit.ViewModels
{
    public sealed class EmptyPlaceholdersViewModel : DialogViewModelBase
    {
        const int ShownNames = 5;

        public EmptyPlaceholdersViewModel(IReadOnlyList<string> emptyPlaceholders, PlaceholderPattern pattern)
            : base("Finalize document")
        {
            List<string> names = emptyPlaceholders.Distinct(StringComparer.Ordinal).ToList();
            Headline = names.Count == 1 ? "1 placeholder is not filled in" : $"{names.Count} placeholders are not filled in";
            NamesText = names.Count <= ShownNames
                ? string.Join(", ", names)
                : string.Join(", ", names.Take(ShownNames - 1)) + $" and {names.Count - ShownNames + 1}\u00A0more";
            QuestionText = $"Keep them as markers, such as {pattern.Format(names[0])}, to fill in later, or delete them from the text?";

            KeepMarkersCommand = new RelayCommand(() => Choose(EmptyPlaceholderAction.KeepMarkers));
            DeleteCommand = new RelayCommand(() => Choose(EmptyPlaceholderAction.Delete));
            CancelCommand = new RelayCommand(() => Close(false));
        }

        public string Headline { get; }

        public string NamesText { get; }

        public string QuestionText { get; }

        public EmptyPlaceholderAction Choice { get; private set; }

        public bool RememberChoice { get; set; }

        public ICommand KeepMarkersCommand { get; }

        public ICommand DeleteCommand { get; }

        public ICommand CancelCommand { get; }

        void Choose(EmptyPlaceholderAction choice)
        {
            Choice = choice;
            Close(true);
        }
    }
}
