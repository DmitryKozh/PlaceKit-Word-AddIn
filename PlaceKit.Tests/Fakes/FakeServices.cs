using System;
using System.Collections.Generic;
using System.Threading;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests.Fakes
{
    sealed class MemorySettings : ISettingsService
    {
        public PlaceKitSettings Current { get; } = new PlaceKitSettings();

        public PlaceKitSettings Saved { get; private set; }

        public string Location => @"HKEY_CURRENT_USER\Software\BoringJob\PlaceKit";

        public PlaceKitSettings Load() => new PlaceKitSettings
        {
            OpeningMarker = Current.OpeningMarker,
            ClosingMarker = Current.ClosingMarker,
            ShowCreateSummary = Current.ShowCreateSummary,
            OpenEditorAfterCreate = Current.OpenEditorAfterCreate,
            EditorPlacement = Current.EditorPlacement,
        };

        public void Save(PlaceKitSettings settings) => Saved = settings;
    }

    sealed class InlineProgress : IProgressService
    {
        public T Run<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, T> operation) =>
            operation(new ProgressRecorder(), CancellationToken.None);
    }

    sealed class FakePanes : ITaskPaneService
    {
        public List<string> Calls { get; } = new List<string>();

        public EditorViewModel Shown { get; private set; }

        public void Show(EditorViewModel editor, Word.Window window)
        {
            Calls.Add(window == null ? "show without window" : "show");
            Shown = editor;
        }

        public void Hide(EditorViewModel editor) => Calls.Add("hide");

        public void Remove(EditorViewModel editor) => Calls.Add("remove");
    }

    sealed class FakeUrlLauncher : IUrlLauncher
    {
        public List<Uri> Opened { get; } = new List<Uri>();

        public void Open(Uri url) => Opened.Add(url);
    }
}
