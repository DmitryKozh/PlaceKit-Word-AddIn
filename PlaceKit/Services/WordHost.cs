using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PlaceKit.Interfaces;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class WordHost : IWordHost
    {
        readonly Func<Word.Application> _application;

        public WordHost(Func<Word.Application> application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        public Word.Document ActiveDocument
        {
            get
            {
                try
                {
                    Word.Application application = _application();
                    return application.Documents.Count > 0 ? application.ActiveDocument : null;
                }
                catch (COMException)
                {
                    // Protected View windows throw here.
                    return null;
                }
            }
        }

        public IntPtr MainWindowHandle
        {
            get
            {
                try
                {
                    return new IntPtr(_application().ActiveWindow.Hwnd);
                }
                catch (COMException)
                {
                    return Process.GetCurrentProcess().MainWindowHandle;
                }
            }
        }

        public Word.Document NewDocument() => _application().Documents.Add();
    }
}
