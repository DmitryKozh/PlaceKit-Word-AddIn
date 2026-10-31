using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using PlaceKit.Tools;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class PlaceholderService : IPlaceholderService
    {
        const int WindowNotActive = unchecked((int)0x800A11FD);

        public PlaceholderOperationResult CreatePlaceholders(Word.Document document, PlaceholderPattern pattern, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<Word.Range> stories = WordStories.GetTextStories(document).ToList();
            ProgressTracker tracker = new ProgressTracker(stories.Sum(story => pattern.CountCandidates(story.Text)), progress);

            using (WordEditScope.Begin(document, "PlaceKit: Create placeholders", untracked: true))
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
            return ModifyEach(document, "PlaceKit: Fill placeholders", untracked: false, targets, progress, token,
                placeholder => WriteValue(placeholder.Control, values[placeholder.Name]));
        }

        public IReadOnlyList<string> GetEmptyPlaceholders(Word.Document document, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<PlaceholderControl> placeholders = FindPlaceholders(document);
            ProgressTracker tracker = new ProgressTracker(placeholders.Count, progress);
            List<string> empty = new List<string>();

            foreach (PlaceholderControl placeholder in placeholders)
            {
                token.ThrowIfCancellationRequested();
                if (IsEmpty(placeholder.Control))
                {
                    empty.Add(placeholder.Name);
                }

                tracker.Add(succeeded: true);
            }

            return empty;
        }

        public PlaceholderOperationResult FinalizePlaceholders(Word.Document document, PlaceholderPattern pattern, EmptyPlaceholderAction emptyPlaceholders,
            IProgress<ProgressInfo> progress, CancellationToken token)
        {
            return ModifyEach(document, "PlaceKit: Finalize placeholders", untracked: true, FindPlaceholders(document), progress, token,
                placeholder => ConvertToText(placeholder.Control, emptyPlaceholders == EmptyPlaceholderAction.KeepMarkers ? pattern.Format(placeholder.Name) : null));
        }

        public string GetText(Word.Document document)
        {
            // A header linked to the previous section's is the same text again; it would double the counts.
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            return string.Join("\n\n", WordStories.GetTextStories(document)
                .Select(ReadText)
                .Where(text => !string.IsNullOrWhiteSpace(text) && seen.Add(text)));
        }

        public string GetSelectedText(Word.Document document)
        {
            try
            {
                Word.Range selection = document.ActiveWindow.Selection.Range;
                return selection.Start < selection.End ? ReadText(selection) : null;
            }
            catch (COMException)
            {
                // no window, such as a document opened invisibly
                return null;
            }
        }

        public PlaceholderOperationResult CreatePlaceholdersFromText(Word.Document document, IReadOnlyList<DetectedPlaceholder> placeholders, IProgress<ProgressInfo> progress, CancellationToken token)
        {
            List<Word.Range> stories = WordStories.GetTextStories(document).ToList();
            ProgressTracker tracker = new ProgressTracker(placeholders.Count, progress);

            using (WordEditScope.Begin(document, "PlaceKit: Detect placeholders", untracked: true))
            {
                // The longest first, so "Anna Berg" becomes a placeholder before "Berg" could take part of it.
                foreach (DetectedPlaceholder placeholder in placeholders.OrderByDescending(placeholder => placeholder.Text.Length))
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    int found = 0;
                    int created = 0;
                    string error = null;
                    foreach (Word.Range story in stories)
                    {
                        created += WrapOccurrences(story, placeholder, ref found, ref error);
                    }

                    string problem = created > 0 ? null
                        : found == 0 ? "not in the document as a whole word"
                        : error != null ? "Word refused: " + error
                        : "only inside other content controls";
                    tracker.Add(succeeded: created > 0, problem == null ? null : $"\"{Shorten(placeholder.Text)}\": {problem}");
                }
            }

            return tracker.ToResult(token.IsCancellationRequested);
        }

        public bool SelectText(Word.Document document, string text)
        {
            foreach (Word.Range story in WordStories.GetTextStories(document))
            {
                Word.Range found = FindWholeWords(story, text).FirstOrDefault();
                if (found != null)
                {
                    found.Select();
                    return true;
                }
            }

            return false;
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

        // A content control reads as <placeholder name="..."/>. Its text can't become a placeholder, so Detect must not
        // suggest it, and the AI service sees which parts are placeholders already, apart from markers still to convert.
        static string ReadText(Word.Range range)
        {
            StringBuilder text = new StringBuilder();
            int position = range.Start;
            foreach (Word.ContentControl control in range.ContentControls.Cast<Word.ContentControl>().OrderBy(control => control.Range.Start).ToList())
            {
                // -1 and +1: the control's tags
                int start = control.Range.Start - 1;
                if (start < position)
                {
                    continue;
                }

                text.Append(TextBetween(range, position, start));
                text.Append(DetectRequest.ExistingPlaceholder(NameOf(control)));
                position = Math.Min(control.Range.End + 1, range.End);
            }

            text.Append(TextBetween(range, position, range.End));
            return WordText.ToPlain(text.ToString());
        }

        static string TextBetween(Word.Range range, int start, int end)
        {
            if (start >= end)
            {
                return string.Empty;
            }

            Word.Range part = range.Duplicate;
            part.SetRange(start, end);
            return part.Text ?? string.Empty;
        }

        static string NameOf(Word.ContentControl control) =>
            PlaceholderTag.TryGetName(control.Tag, out string name) ? name
            : !string.IsNullOrWhiteSpace(control.Title) ? control.Title
            : "Field";

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

            int searchFrom = range.Start;
            while (!token.IsCancellationRequested && find.Execute())
            {
                int matchStart = range.Start;
                int resumeAt;

                // Never go back, or this would loop forever.
                if (matchStart < searchFrom)
                {
                    Trace.TraceWarning("PlaceKit: the search for markers went back from {0} to {1}", searchFrom, matchStart);
                    break;
                }

                if (pattern.TryGetName(range.Text, out string name) && TryCreateControl(range, name, out Word.ContentControl control, out _))
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

                searchFrom = ContinueAt(range, resumeAt, story);
            }
        }

        // Text inside another content control is left alone.
        static int WrapOccurrences(Word.Range story, DetectedPlaceholder placeholder, ref int matches, ref string error)
        {
            int created = 0;
            foreach (Word.Range found in FindWholeWords(story, placeholder.Text))
            {
                matches++;
                if (found.ParentContentControl != null || found.ContentControls.Count > 0)
                {
                    continue;
                }

                if (TryCreateControl(found, placeholder.Name, out _, out string failure, keepText: !placeholder.IsMarker))
                {
                    created++;
                }
                else
                {
                    error = failure;
                }
            }

            return created;
        }

        // Word's "whole words only" fails for text with spaces, so the neighbours are checked here.
        // Each match is yielded before moving on, so the caller can wrap it in a control.
        static IEnumerable<Word.Range> FindWholeWords(Word.Range story, string text)
        {
            Word.Range range = story.Duplicate;
            Word.Find find = range.Find;
            find.ClearFormatting();
            find.Text = text.Replace("^", "^^");
            find.MatchCase = true;
            find.MatchWholeWord = false;
            find.MatchWildcards = false;
            find.Forward = true;
            find.Format = false;
            find.Wrap = Word.WdFindWrap.wdFindStop;

            int searchFrom = range.Start;
            while (find.Execute())
            {
                // Never go back, or this would loop forever.
                if (range.Start < searchFrom)
                {
                    Trace.TraceWarning("PlaceKit: the search for '{0}' went back from {1} to {2}", text, searchFrom, range.Start);
                    yield break;
                }

                if (IsWholeWord(range, text))
                {
                    yield return range;
                }

                searchFrom = ContinueAt(range, range.End, story);
            }
        }

        // Continue from an insertion point. A range up to the story end gets widened to whole table rows,
        // and Find keeps returning the same match.
        static int ContinueAt(Word.Range range, int position, Word.Range story)
        {
            int at = Math.Min(position, story.End);
            range.SetRange(at, at);
            return at;
        }

        static bool IsWholeWord(Word.Range found, string text)
        {
            Word.Range before = found.Duplicate;
            before.SetRange(Math.Max(0, found.Start - 1), found.Start);
            Word.Range after = found.Duplicate;
            after.SetRange(found.End, found.End + 1);
            string around = CharacterOf(before) + text + CharacterOf(after);
            return WordText.IsWholeWord(around, 1, text.Length);
        }

        // keepText: the text becomes the value; otherwise the control starts empty (markers).
        static bool TryCreateControl(Word.Range range, string name, out Word.ContentControl control, out string error, bool keepText = false)
        {
            try
            {
                control = AddControl(range);
            }
            catch (COMException ex)
            {
                Trace.TraceWarning("PlaceKit: cannot create placeholder '{0}': {1}", name, ex.Message);
                control = null;
                error = ex.Message;
                return false;
            }

            error = null;

            control.Tag = PlaceholderTag.Create(name);
            control.Title = name;
            control.MultiLine = true;
            control.SetPlaceholderText(Text: name);

            // An empty control shows its placeholder text.
            if (!keepText)
            {
                control.Range.Text = string.Empty;
            }

            return true;
        }

        // Word refuses to add a control while its document window isn't the active one (error 4605), so activate
        // the window and try once more.
        static Word.ContentControl AddControl(Word.Range range)
        {
            try
            {
                return range.ContentControls.Add(Word.WdContentControlType.wdContentControlText, range);
            }
            catch (COMException ex) when (ex.ErrorCode == WindowNotActive && TryActivateWindow(range.Document))
            {
                return range.ContentControls.Add(Word.WdContentControlType.wdContentControlText, range);
            }
        }

        static bool TryActivateWindow(Word.Document document)
        {
            try
            {
                document.ActiveWindow.Activate();
                return true;
            }
            catch (COMException)
            {
                // a document opened without a window
                return false;
            }
        }

        static string Shorten(string text) => text.Length <= 60 ? text : text.Substring(0, 57) + "...";

        static string ReadValue(Word.ContentControl control) =>
            control.ShowingPlaceholderText ? string.Empty : WordText.ToDisplay(TextWithoutDeletions(control.Range));

        // Also empty when a tracked deletion is all that is left in it.
        static bool IsEmpty(Word.ContentControl control) =>
            control.ShowingPlaceholderText || TextWithoutDeletions(control.Range).Length == 0;

        // The text as it reads once the tracked changes are accepted; Range.Text includes deleted text.
        // In a plain-text control each character is one position, so offsets match.
        static string TextWithoutDeletions(Word.Range range)
        {
            string text = range.Text ?? string.Empty;
            List<Word.Range> deletions = range.Revisions.Cast<Word.Revision>()
                .Where(revision => revision.Type == Word.WdRevisionType.wdRevisionDelete)
                .Select(revision => revision.Range)
                .OrderByDescending(deleted => deleted.Start)
                .ToList();

            StringBuilder kept = new StringBuilder(text);
            foreach (Word.Range deleted in deletions)
            {
                int start = Math.Max(deleted.Start, range.Start) - range.Start;
                int end = Math.Min(Math.Min(deleted.End, range.End) - range.Start, kept.Length);
                if (end > start)
                {
                    kept.Remove(start, end - start);
                }
            }

            return kept.ToString();
        }

        static void WriteValue(Word.ContentControl control, string value)
        {
            control.LockContents = false;
            if (control.Type == Word.WdContentControlType.wdContentControlText)
            {
                control.MultiLine = true;
            }

            control.Range.Text = WordText.ToWord(value);
        }

        // An empty control becomes its marker again, or is deleted when there is no marker.
        static void ConvertToText(Word.ContentControl control, string emptyMarker)
        {
            control.LockContentControl = false;
            control.LockContents = false;
            if (!IsEmpty(control))
            {
                control.Delete(DeleteContents: false);
            }
            else if (emptyMarker != null)
            {
                control.Range.Text = emptyMarker;
                control.Delete(DeleteContents: false);
            }
            else
            {
                // Where the control starts. Word may remove a space next to it too (smart cut),
                // so the neighbours are read after the deletion.
                Word.Range gap = control.Range.Duplicate;
                gap.SetRange(control.Range.Start - 1, control.Range.Start - 1);
                control.Delete(DeleteContents: true);
                try
                {
                    RemoveSpaceLeftBehind(gap);
                }
                catch (COMException ex)
                {
                    // The placeholder is gone; a space left next to it is only cosmetic.
                    Trace.TraceWarning("PlaceKit: cannot remove the space next to a deleted placeholder: {0}", ex.Message);
                }
            }
        }

        // "Dear {{Name}}, see" becomes "Dear, see", not "Dear , see", "a {{B}} c" keeps one space,
        // and no space is left at the start or the end of a line.
        static void RemoveSpaceLeftBehind(Word.Range gap)
        {
            const string LineEnds = "\r\a\v";
            Word.Range before = gap.Duplicate;
            before.SetRange(Math.Max(0, gap.Start - 1), gap.Start);
            Word.Range after = gap.Duplicate;
            after.SetRange(gap.Start, gap.Start + 1);
            char previous = CharacterOf(before);
            char next = CharacterOf(after);

            // Range.Delete would let Word's smart cut put the space back.
            if (previous == ' ' && (" ,.;:!?)" + LineEnds).IndexOf(next) >= 0)
            {
                before.Text = string.Empty;
            }
            else if (next == ' ' && LineEnds.IndexOf(previous) >= 0)
            {
                after.Text = string.Empty;
            }
        }

        // The start or the end of the story counts as a line end; another control's tag reads as no text.
        static char CharacterOf(Word.Range range) =>
            range.Start == range.End ? '\r' : string.IsNullOrEmpty(range.Text) ? '\0' : range.Text[0];

        static PlaceholderOperationResult ModifyEach(Word.Document document, string undoEntryName, bool untracked, IReadOnlyCollection<PlaceholderControl> placeholders,
            IProgress<ProgressInfo> progress, CancellationToken token, Action<PlaceholderControl> modify)
        {
            ProgressTracker tracker = new ProgressTracker(placeholders.Count, progress);
            using (WordEditScope.Begin(document, undoEntryName, untracked))
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
