using PlaceKit.Models;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    // Call on Word's main thread.
    public interface IGuideService
    {
        Word.Document CreateGuide(PlaceholderPattern pattern);
    }
}
