using PlaceKit.Models;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    // The document of the Guide command, to try PlaceKit on. Call on Word's main thread.
    public interface IGuideService
    {
        // Opens a new document with the steps to try and a sample claim, its placeholders marked with the pattern.
        Word.Document CreateGuide(PlaceholderPattern pattern);
    }
}
