using System;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IWordHost
    {
        Word.Document ActiveDocument { get; }

        IntPtr MainWindowHandle { get; }

        // Opens a new blank document and makes it the active one.
        Word.Document NewDocument();
    }
}
