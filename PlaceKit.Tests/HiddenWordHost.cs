using System;
using PlaceKit.Interfaces;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests
{
    // The add-in's WordHost takes a Func of an embedded interop type, which can't cross assemblies.
    sealed class HiddenWordHost : IWordHost
    {
        public Word.Document ActiveDocument => WordInstance.Application.ActiveDocument;

        public IntPtr MainWindowHandle => IntPtr.Zero;

        public bool IsUserSession => false;

        public Word.Document NewDocument() => WordInstance.NewDocument();
    }
}
