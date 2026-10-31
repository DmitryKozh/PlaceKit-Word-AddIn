using System;
using System.Collections.Generic;
using System.Threading;
using PlaceKit.Models;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Interfaces
{
    // Call on Word's main thread. Each change is a single undo entry.
    public interface IPlaceholderService
    {
        PlaceholderOperationResult CreatePlaceholders(Word.Document document, PlaceholderPattern pattern, IProgress<ProgressInfo> progress, CancellationToken token);

        IReadOnlyList<PlaceholderInfo> GetPlaceholders(Word.Document document, IProgress<ProgressInfo> progress, CancellationToken token);

        PlaceholderOperationResult UpdatePlaceholders(Word.Document document, IReadOnlyDictionary<string, string> values, IProgress<ProgressInfo> progress, CancellationToken token);

        // once per occurrence, in document order
        IReadOnlyList<string> GetEmptyPlaceholders(Word.Document document, IProgress<ProgressInfo> progress, CancellationToken token);

        PlaceholderOperationResult FinalizePlaceholders(Word.Document document, PlaceholderPattern pattern, EmptyPlaceholderAction emptyPlaceholders, IProgress<ProgressInfo> progress, CancellationToken token);

        bool SelectPlaceholder(Word.Document document, string name, int occurrence);

        // Content controls read as markers with their names.
        string GetText(Word.Document document, PlaceholderPattern pattern);

        // null when nothing is selected.
        string GetSelectedText(Word.Document document, PlaceholderPattern pattern);

        // Processed: texts that became at least one placeholder. Skipped: texts not found.
        PlaceholderOperationResult CreatePlaceholdersFromText(Word.Document document, IReadOnlyList<DetectedPlaceholder> placeholders, IProgress<ProgressInfo> progress, CancellationToken token);

        bool SelectText(Word.Document document, string text);
    }
}
