using System.Collections.Generic;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tools
{
    // Document.Content only covers the main story. Headers, footers, text boxes
    // and notes are separate stories.
    internal static class WordStories
    {
        static readonly HashSet<Word.WdStoryType> TextStories = new HashSet<Word.WdStoryType>
        {
            Word.WdStoryType.wdMainTextStory,
            Word.WdStoryType.wdFootnotesStory,
            Word.WdStoryType.wdEndnotesStory,
            Word.WdStoryType.wdTextFrameStory,
            Word.WdStoryType.wdPrimaryHeaderStory,
            Word.WdStoryType.wdEvenPagesHeaderStory,
            Word.WdStoryType.wdFirstPageHeaderStory,
            Word.WdStoryType.wdPrimaryFooterStory,
            Word.WdStoryType.wdEvenPagesFooterStory,
            Word.WdStoryType.wdFirstPageFooterStory,
        };

        public static IEnumerable<Word.Range> GetTextStories(Word.Document document)
        {
            foreach (Word.Range story in document.StoryRanges)
            {
                if (!TextStories.Contains(story.StoryType))
                {
                    continue;
                }

                for (Word.Range current = story; current != null; current = current.NextStoryRange)
                {
                    yield return current;
                }
            }
        }
    }
}
