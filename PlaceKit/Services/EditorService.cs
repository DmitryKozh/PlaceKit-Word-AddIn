using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;
using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class EditorService : IEditorService
    {
        readonly IPlaceholderService _placeholders;
        readonly IProgressService _progress;
        readonly IDialogService _dialogs;
        readonly ISettingsService _settings;
        readonly ITaskPaneService _panes;
        readonly IPlaceholderValuesFile _valuesFile;
        readonly Dictionary<EditorViewModel, Word.Document> _documents = new Dictionary<EditorViewModel, Word.Document>();

        public EditorService(IPlaceholderService placeholders, IProgressService progress, IDialogService dialogs, ISettingsService settings, ITaskPaneService panes, IPlaceholderValuesFile valuesFile)
        {
            _placeholders = placeholders;
            _progress = progress;
            _dialogs = dialogs;
            _settings = settings;
            _panes = panes;
            _valuesFile = valuesFile;
        }

        public void Open(Word.Document document)
        {
            RemoveClosedDocuments();

            // Windows are modal, so an editor that is still open is in the side panel.
            EditorViewModel editor = Find(document);
            if (editor != null)
            {
                editor.Reload();
                ShowInPane(editor);
                return;
            }

            PlaceKitSettings settings = _settings.Load();
            editor = new EditorViewModel(new PlaceholderDocument(document, _placeholders, _progress), _dialogs, _valuesFile);
            editor.Reload();
            if (editor.IsEmpty)
            {
                string example = settings.CreatePattern().Format("text");
                _dialogs.ShowMessage("PlaceKit", $"This document has no placeholders yet. Click Create to turn {example} markers into placeholders.", MessageKind.Information);
                return;
            }

            _documents.Add(editor, document);
            editor.CloseRequested += HandleCloseRequested;
            if (settings.EditorPlacement == EditorPlacement.Window)
            {
                ShowInWindow(editor);
            }
            else
            {
                ShowInPane(editor);
            }
        }

        public void Refresh(Word.Document document)
        {
            RemoveClosedDocuments();
            Find(document)?.Reload();
        }

        public void FinalizeDocument(Word.Document document)
        {
            RemoveClosedDocuments();
            EditorViewModel editor = Find(document);
            if (editor != null && editor.HasChanges)
            {
                string question = $"The editor has {TextFormat.Quantity(editor.ModifiedCount, "unsaved change")}. Save them and finalize the document?";
                if (!_dialogs.Confirm("Unsaved changes", question) || !editor.SaveChanges())
                {
                    return;
                }
            }

            if (TryFinalize(document))
            {
                if (editor != null)
                {
                    End(editor);
                }
            }
            else
            {
                editor?.Reload();
            }
        }

        void ShowInPane(EditorViewModel editor)
        {
            editor.AttachTo(EditorPlacement.SidePanel);
            _panes.Show(editor, _documents[editor].ActiveWindow);
        }

        void ShowInWindow(EditorViewModel editor)
        {
            _panes.Hide(editor);
            editor.AttachTo(EditorPlacement.Window);
            _dialogs.ShowDialog(editor);
            HandleClosed(editor);
        }

        // A window is handled when ShowDialog returns.
        void HandleCloseRequested(object sender, DialogCloseRequestedEventArgs e)
        {
            EditorViewModel editor = (EditorViewModel)sender;
            if (editor.Placement == EditorPlacement.SidePanel)
            {
                HandleClosed(editor);
            }
        }

        void HandleClosed(EditorViewModel editor)
        {
            if (editor.RequestedPlacement == EditorPlacement.Window)
            {
                ShowInWindow(editor);
            }
            else if (editor.RequestedPlacement == EditorPlacement.SidePanel)
            {
                ShowInPane(editor);
            }
            else if (editor.FinalizeRequested)
            {
                FinalizeFromEditor(editor);
            }
            else
            {
                End(editor);
            }
        }

        void FinalizeFromEditor(EditorViewModel editor)
        {
            bool finalized;
            try
            {
                finalized = TryFinalize(_documents[editor]);
            }
            catch
            {
                // The command reports the error. The request must not stay set, and a closed window must not leave its editor behind.
                if (editor.IsInWindow)
                {
                    End(editor);
                }
                else
                {
                    editor.AttachTo(EditorPlacement.SidePanel);
                }

                throw;
            }

            if (finalized)
            {
                End(editor);
                return;
            }

            // Canceled: back to the editor, which shows the values it saved.
            editor.Reload();
            if (editor.IsInWindow)
            {
                ShowInWindow(editor);
            }
            else
            {
                ShowInPane(editor);
            }
        }

        // False when the user cancels the check or the question about empty placeholders.
        bool TryFinalize(Word.Document document)
        {
            PlaceKitSettings settings = _settings.Load();
            IReadOnlyList<string> empty;
            try
            {
                empty = _progress.Run("Checking placeholders", (progress, token) => _placeholders.GetEmptyPlaceholders(document, progress, token));
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            EmptyPlaceholderAction? action = empty.Count == 0 ? settings.EmptyPlaceholderAction : ChooseForEmpty(empty, settings);
            if (action == null)
            {
                return false;
            }

            PlaceholderPattern pattern = settings.CreatePattern();
            PlaceholderOperationResult result = _progress.Run("Finalizing placeholders",
                (progress, token) => _placeholders.FinalizePlaceholders(document, pattern, action.Value, progress, token));

            if (result.WasCanceled)
            {
                ShowMessage(TextFormat.Canceled(result.Processed, "finalized"), MessageKind.Information);
            }
            else if (result.Processed == 0 && result.Skipped == 0)
            {
                ShowMessage("This document has no placeholders to finalize.", MessageKind.Information);
            }
            else if (result.Skipped > 0)
            {
                ShowMessage($"{TextFormat.Quantity(result.Processed, "placeholder")} finalized, {result.Skipped} could not be converted.", MessageKind.Warning);
            }
            else
            {
                string emptyText = action == EmptyPlaceholderAction.Delete ? "deleted" : "turned back into markers";
                ShowMessage($"{TextFormat.Quantity(result.Processed - empty.Count, "placeholder")} converted to regular text" +
                    (empty.Count == 0 ? "." : $", {TextFormat.Quantity(empty.Count, "empty one")} {emptyText}."), MessageKind.Information);
            }

            return true;
        }

        // Null when the user cancels.
        EmptyPlaceholderAction? ChooseForEmpty(IReadOnlyList<string> empty, PlaceKitSettings settings)
        {
            if (!settings.AskAboutEmptyPlaceholders)
            {
                return settings.EmptyPlaceholderAction;
            }

            EmptyPlaceholdersViewModel question = new EmptyPlaceholdersViewModel(empty, settings.CreatePattern());
            if (_dialogs.ShowDialog(question) != true)
            {
                return null;
            }

            if (question.RememberChoice)
            {
                settings.AskAboutEmptyPlaceholders = false;
                settings.EmptyPlaceholderAction = question.Choice;
                _settings.Save(settings);
            }

            return question.Choice;
        }

        void ShowMessage(string message, MessageKind kind) => _dialogs.ShowMessage("PlaceKit", message, kind);

        void End(EditorViewModel editor)
        {
            editor.CloseRequested -= HandleCloseRequested;
            _panes.Remove(editor);
            _documents.Remove(editor);
        }

        EditorViewModel Find(Word.Document document) =>
            _documents.FirstOrDefault(entry => entry.Value == document).Key;

        // Word removes the pane together with the window.
        void RemoveClosedDocuments()
        {
            foreach (EditorViewModel editor in _documents.Where(entry => !IsOpen(entry.Value)).Select(entry => entry.Key).ToList())
            {
                End(editor);
            }
        }

        static bool IsOpen(Word.Document document)
        {
            try
            {
                return document.FullName != null;
            }
            catch (COMException)
            {
                return false;
            }
            catch (InvalidComObjectException)
            {
                return false;
            }
        }
    }
}
