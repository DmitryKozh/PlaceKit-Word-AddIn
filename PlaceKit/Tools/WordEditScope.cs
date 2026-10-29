using System;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tools
{
    // One undo entry and no screen updates for a PlaceKit edit.
    internal sealed class WordEditScope : IDisposable
    {
        readonly Word.Document _document;
        readonly Word.Application _application;
        readonly bool _screenUpdating;
        readonly bool _resumeTracking;
        readonly bool _ownsUndoRecord;

        WordEditScope(Word.Document document, string undoEntryName, bool untracked)
        {
            _document = document;
            _application = document.Application;

            // First, while nothing else has changed: Word refuses this when the document only allows tracked changes.
            if (untracked && document.TrackRevisions)
            {
                document.TrackRevisions = false;
                _resumeTracking = true;
            }

            _screenUpdating = _application.ScreenUpdating;
            _application.ScreenUpdating = false;

            Word.UndoRecord undoRecord = _application.UndoRecord;
            if (!undoRecord.IsRecordingCustomRecord)
            {
                undoRecord.StartCustomRecord(undoEntryName);
                _ownsUndoRecord = true;
            }
        }

        // untracked: Track Changes is off while the scope is open. Creating and finalizing placeholders use it, because
        // a tracked edit keeps the old text as a deleted revision and a control would never become empty.
        public static WordEditScope Begin(Word.Document document, string undoEntryName, bool untracked = false) =>
            new WordEditScope(document, undoEntryName, untracked);

        public void Dispose()
        {
            try
            {
                if (_ownsUndoRecord)
                {
                    _application.UndoRecord.EndCustomRecord();
                }
            }
            finally
            {
                try
                {
                    _application.ScreenUpdating = _screenUpdating;
                }
                finally
                {
                    if (_resumeTracking)
                    {
                        _document.TrackRevisions = true;
                    }
                }
            }
        }
    }
}
