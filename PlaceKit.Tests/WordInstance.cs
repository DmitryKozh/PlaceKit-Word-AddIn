using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tests
{
    // One hidden Word shared by all tests in the "Word" category. Starting Word takes a second or two.
    [TestClass]
    public class WordInstance
    {
        public const string Category = "Word";

        static Word.Application _application;

        public static Word.Application Application
        {
            get
            {
                if (_application == null)
                {
                    Type type = Type.GetTypeFromProgID("Word.Application");
                    if (type == null)
                    {
                        Assert.Inconclusive("Microsoft Word is not installed.");
                    }

                    _application = (Word.Application)Activator.CreateInstance(type);
                }

                return _application;
            }
        }

        public static Word.Document NewDocument(string text = null)
        {
            Word.Document document = Application.Documents.Add();
            if (text != null)
            {
                document.Content.Text = text;
            }

            return document;
        }

        public static void CloseDocuments()
        {
            if (_application == null)
            {
                return;
            }

            foreach (Word.Document document in _application.Documents)
            {
                ((Word._Document)document).Close(Word.WdSaveOptions.wdDoNotSaveChanges);
            }
        }

        // All stories with their linked parts (each section's headers, every text box).
        public static List<Word.ContentControl> PlaceKitControls(Word.Document document)
        {
            var ids = new HashSet<string>();
            var controls = new List<Word.ContentControl>();
            foreach (Word.Range story in document.StoryRanges)
            {
                for (Word.Range range = story; range != null; range = range.NextStoryRange)
                {
                    foreach (Word.ContentControl control in range.ContentControls)
                    {
                        if (ids.Add(control.ID) && (control.Tag ?? "").StartsWith("PlaceKit-", StringComparison.Ordinal))
                        {
                            controls.Add(control);
                        }
                    }
                }
            }

            return controls;
        }

        [AssemblyCleanup]
        public static void Quit()
        {
            if (_application == null)
            {
                return;
            }

            CloseDocuments();
            ((Word._Application)_application).Quit(Word.WdSaveOptions.wdDoNotSaveChanges);
            Marshal.ReleaseComObject(_application);
            _application = null;
        }
    }
}
