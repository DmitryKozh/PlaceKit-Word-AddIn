using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IEditorService
    {
        void Open(Word.Document document);

        void Refresh(Word.Document document);

        // Turns the placeholders into regular text, asking about empty ones unless a choice is remembered,
        // and closes the document's editor.
        void FinalizeDocument(Word.Document document);
    }
}
