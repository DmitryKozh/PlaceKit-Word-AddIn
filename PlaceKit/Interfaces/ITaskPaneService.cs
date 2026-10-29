using PlaceKit.ViewModels;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    public interface ITaskPaneService
    {
        void Show(EditorViewModel editor, Word.Window window);

        void Hide(EditorViewModel editor);

        void Remove(EditorViewModel editor);
    }
}
