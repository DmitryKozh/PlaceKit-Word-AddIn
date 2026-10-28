using System;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tools
{
    internal sealed class WordEditScope : IDisposable
    {
        readonly Word.Application _application;
        readonly bool _screenUpdating;
        readonly bool _ownsUndoRecord;

        WordEditScope(Word.Application application, string undoEntryName)
        {
            _application = application;

            Word.UndoRecord undoRecord = application.UndoRecord;
            if (!undoRecord.IsRecordingCustomRecord)
            {
                undoRecord.StartCustomRecord(undoEntryName);
                _ownsUndoRecord = true;
            }

            _screenUpdating = application.ScreenUpdating;
            application.ScreenUpdating = false;
        }

        public static WordEditScope Begin(Word.Document document, string undoEntryName) =>
            new WordEditScope(document.Application, undoEntryName);

        public void Dispose()
        {
            _application.ScreenUpdating = _screenUpdating;
            if (_ownsUndoRecord)
            {
                _application.UndoRecord.EndCustomRecord();
            }
        }
    }
}
