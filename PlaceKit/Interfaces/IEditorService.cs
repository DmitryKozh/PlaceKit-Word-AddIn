using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface IEditorService
    {
        void Open(Word.Document document);

        void Refresh(Word.Document document);
    }
}
