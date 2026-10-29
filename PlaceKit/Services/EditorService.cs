using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using PlaceKit.Interfaces;
using PlaceKit.Models;
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

            if (editor.RequestedPlacement == EditorPlacement.SidePanel)
            {
                ShowInPane(editor);
            }
            else
            {
                End(editor);
            }
        }

        void HandleCloseRequested(object sender, DialogCloseRequestedEventArgs e)
        {
            EditorViewModel editor = (EditorViewModel)sender;
            if (editor.Placement != EditorPlacement.SidePanel)
            {
                return;
            }

            if (editor.RequestedPlacement == EditorPlacement.Window)
            {
                ShowInWindow(editor);
            }
            else
            {
                End(editor);
            }
        }

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
