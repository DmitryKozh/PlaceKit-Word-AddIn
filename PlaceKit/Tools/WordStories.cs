using System.Collections.Generic;
using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Tools
{
    // Document.Content only covers the main story. Headers, footers, text boxes
    // and notes are separate stories.
    internal static class WordStories
    {
        static readonly HashSet<Word.WdStoryType> HeaderAndFooterStories = new HashSet<Word.WdStoryType>
        {
            Word.WdStoryType.wdPrimaryHeaderStory,
            Word.WdStoryType.wdEvenPagesHeaderStory,
            Word.WdStoryType.wdFirstPageHeaderStory,
            Word.WdStoryType.wdPrimaryFooterStory,
            Word.WdStoryType.wdEvenPagesFooterStory,
            Word.WdStoryType.wdFirstPageFooterStory,
        };

        static readonly HashSet<Word.WdStoryType> TextStories = new HashSet<Word.WdStoryType>(HeaderAndFooterStories)
        {
            Word.WdStoryType.wdMainTextStory,
            Word.WdStoryType.wdFootnotesStory,
            Word.WdStoryType.wdEndnotesStory,
            Word.WdStoryType.wdTextFrameStory,
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

                    if (HeaderAndFooterStories.Contains(current.StoryType))
                    {
                        foreach (Word.Range textBox in TextBoxesIn(current))
                        {
                            yield return textBox;
                        }
                    }
                }
            }
        }

        // The text box story only links the body's text boxes; those in a header or footer hang off its range.
        static List<Word.Range> TextBoxesIn(Word.Range headerOrFooter)
        {
            List<Word.Range> textBoxes = new List<Word.Range>();
            foreach (Word.Shape shape in headerOrFooter.ShapeRange)
            {
                try
                {
                    if (shape.TextFrame.HasText != 0)
                    {
                        textBoxes.Add(shape.TextFrame.TextRange);
                    }
                }
                catch (COMException)
                {
                    // a picture or another shape without a text frame
                }
            }

            return textBoxes;
        }
    }
}
