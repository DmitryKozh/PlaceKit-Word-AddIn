using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class PlaceholderService : IPlaceholderService
    {
        public PlaceholderOperationResult CreatePlaceholders(Word.Document document, PlaceholderPattern pattern, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<Word.Range> stories = WordStories.GetTextStories(document).ToList();
            ProgressTracker tracker = new ProgressTracker(stories.Sum(story => pattern.CountCandidates(story.Text)), progress);

            using (WordEditScope.Begin(document, "PlaceKit: Create placeholders"))
            {
                foreach (Word.Range story in stories.TakeWhile(story => !token.IsCancellationRequested))
                {
                    ConvertMarkers(story, pattern, tracker, token);
                }
            }

            return tracker.ToResult(token.IsCancellationRequested);
        }

        public IReadOnlyList<PlaceholderInfo> GetPlaceholders(Word.Document document, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<PlaceholderControl> placeholders = FindPlaceholders(document);
            ProgressTracker tracker = new ProgressTracker(placeholders.Count, progress);
            List<KeyValuePair<string, string>> values = new List<KeyValuePair<string, string>>(placeholders.Count);

            foreach (PlaceholderControl placeholder in placeholders)
            {
                token.ThrowIfCancellationRequested();
                values.Add(new KeyValuePair<string, string>(placeholder.Name, ReadValue(placeholder.Control)));
                tracker.Add(succeeded: true);
            }

            return values
                .GroupBy(value => value.Key, value => value.Value, StringComparer.Ordinal)
                .Select(group => new PlaceholderInfo(group.Key, group.First(), group.Count(), group.Distinct().Skip(1).Any()))
                .ToList();
        }

        public PlaceholderOperationResult UpdatePlaceholders(Word.Document document, IReadOnlyDictionary<string, string> values, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<PlaceholderControl> targets = FindPlaceholders(document).Where(placeholder => values.ContainsKey(placeholder.Name)).ToList();
            return ModifyEach(document, "PlaceKit: Fill placeholders", targets, progress, token,
                placeholder => WriteValue(placeholder.Control, values[placeholder.Name]));
        }

        public PlaceholderOperationResult ClearPlaceholders(Word.Document document, PlaceholderPattern pattern, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            return ModifyEach(document, "PlaceKit: Clear placeholders", FindPlaceholders(document), progress, token,
                placeholder => ConvertToText(placeholder.Control, pattern.Format(placeholder.Name)));
        }

        public bool SelectPlaceholder(Word.Document document, string name, int occurrence)
        {
            List<PlaceholderControl> matches = FindPlaceholders(document).Where(placeholder => placeholder.Name == name).ToList();
            if (matches.Count == 0)
            {
                return false;
            }

            try
            {
                matches[occurrence % matches.Count].Control.Range.Select();
                return true;
            }
            catch (COMException ex)
            {
                Trace.TraceWarning("PlaceKit: cannot select placeholder '{0}': {1}", name, ex.Message);
                return false;
            }
        }

        static void ConvertMarkers(Word.Range story, PlaceholderPattern pattern, ProgressTracker tracker, CancellationToken token)
        {
            Word.Range range = story.Duplicate;
            Word.Find find = range.Find;
            find.ClearFormatting();
            find.Text = pattern.ToWordWildcards();
            find.MatchWildcards = true;
            find.Forward = true;
            find.Format = false;
            find.Wrap = Word.WdFindWrap.wdFindStop;

            // Search the rest of the story explicitly: a collapsed range inside the new
            // control would only search inside that control.
            while (!token.IsCancellationRequested && find.Execute())
            {
                int matchStart = range.Start;
                int resumeAt;

                if (pattern.TryGetName(range.Text, out string name) && TryCreateControl(range, name, out Word.ContentControl control))
                {
                    tracker.Add(succeeded: true);

                    // +1 skips the control's end tag.
                    resumeAt = control.Range.End + 1;
                }
                else
                {
                    // "{{ broken {{Name}}" still contains a valid marker, so resume after the opening marker.
                    tracker.Add(succeeded: false);
                    resumeAt = matchStart + pattern.Opening.Length;
                }

                range.SetRange(Math.Min(resumeAt, story.End), story.End);
            }
        }

        static bool TryCreateControl(Word.Range marker, string name, out Word.ContentControl control)
        {
            try
            {
                control = marker.ContentControls.Add(Word.WdContentControlType.wdContentControlText, marker);
            }
            catch (COMException ex)
            {
                Trace.TraceWarning("PlaceKit: cannot create placeholder '{0}': {1}", name, ex.Message);
                control = null;
                return false;
            }

            control.Tag = PlaceholderTag.Create(name);
            control.Title = name;
            control.MultiLine = true;
            control.SetPlaceholderText(Text: name);

            // An empty control shows its placeholder text.
            control.Range.Text = string.Empty;
            return true;
        }

        static string ReadValue(Word.ContentControl control) =>
            control.ShowingPlaceholderText ? string.Empty : WordText.ToDisplay(control.Range.Text);

        static void WriteValue(Word.ContentControl control, string value)
        {
            control.LockContents = false;
            if (control.Type == Word.WdContentControlType.wdContentControlText)
            {
                control.MultiLine = true;
            }

            control.Range.Text = WordText.ToWord(value);
        }

        static void ConvertToText(Word.ContentControl control, string marker)
        {
            control.LockContentControl = false;
            control.LockContents = false;
            if (control.ShowingPlaceholderText)
            {
                control.Range.Text = marker;
            }

            control.Delete(DeleteContents: false);
        }

        static PlaceholderOperationResult ModifyEach(Word.Document document, string undoEntryName, IReadOnlyCollection<PlaceholderControl> placeholders,
            IProgress<ProgressInfo> progress, CancellationToken token, Action<PlaceholderControl> modify)
        {
            ProgressTracker tracker = new ProgressTracker(placeholders.Count, progress);
            using (WordEditScope.Begin(document, undoEntryName))
            {
                foreach (PlaceholderControl placeholder in placeholders)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    tracker.Add(TryModify(placeholder, modify));
                }
            }

            return tracker.ToResult(token.IsCancellationRequested);
        }

        static bool TryModify(PlaceholderControl placeholder, Action<PlaceholderControl> modify)
        {
            try
            {
                modify(placeholder);
                return true;
            }
            catch (COMException ex)
            {
                Trace.TraceWarning("PlaceKit: cannot modify placeholder '{0}': {1}", placeholder.Name, ex.Message);
                return false;
            }
        }

        // Collected up front: deleting controls while enumerating Word's collections skips some.
        static List<PlaceholderControl> FindPlaceholders(Word.Document document)
        {
            HashSet<string> seenIds = new HashSet<string>(StringComparer.Ordinal);
            List<PlaceholderControl> placeholders = new List<PlaceholderControl>();

            foreach (Word.Range story in WordStories.GetTextStories(document))
            {
                foreach (Word.ContentControl control in story.ContentControls)
                {
                    // Linked headers return the same control more than once.
                    if (seenIds.Add(control.ID) && PlaceholderTag.TryGetName(control.Tag, out string name))
                    {
                        placeholders.Add(new PlaceholderControl(control, name));
                    }
                }
            }

            return placeholders;
        }

        sealed class PlaceholderControl
        {
            public PlaceholderControl(Word.ContentControl control, string name)
            {
                Control = control;
                Name = name;
            }

            public Word.ContentControl Control { get; }

            public string Name { get; }
        }
    }
}
