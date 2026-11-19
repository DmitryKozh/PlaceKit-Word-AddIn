using System;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IWordHost
    {
        Word.Document ActiveDocument { get; }

        IntPtr MainWindowHandle { get; }

        // false when a program runs Word, perhaps without a window, and nobody would answer a dialog
        bool IsUserSession { get; }

        Word.Document NewDocument();
    }
}
