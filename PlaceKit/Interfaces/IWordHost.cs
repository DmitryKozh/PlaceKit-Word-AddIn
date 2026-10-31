using System;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IWordHost
    {
        Word.Document ActiveDocument { get; }

        IntPtr MainWindowHandle { get; }

        Word.Document NewDocument();
    }
}
