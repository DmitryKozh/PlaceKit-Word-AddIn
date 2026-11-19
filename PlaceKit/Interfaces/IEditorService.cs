using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IEditorService
    {
        void Open(Word.Document document);

        void Refresh(Word.Document document);

        // Also closes the document's editor.
        void FinalizeDocument(Word.Document document);

        // false: the user wants to keep the document open
        bool CanCloseDocument(Word.Document document);
    }
}
