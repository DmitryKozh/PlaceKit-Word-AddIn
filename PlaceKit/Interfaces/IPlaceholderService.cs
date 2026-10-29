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

        // The names of the placeholders that are not filled in, once per occurrence, in document order.
        IReadOnlyList<string> GetEmptyPlaceholders(Word.Document document, IProgress<ProgressInfo> progress, CancellationToken token);

        // Turns the placeholders into regular text; empty ones become markers again or are deleted.
        PlaceholderOperationResult FinalizePlaceholders(Word.Document document, PlaceholderPattern pattern, EmptyPlaceholderAction emptyPlaceholders, IProgress<ProgressInfo> progress, CancellationToken token);

        bool SelectPlaceholder(Word.Document document, string name, int occurrence);
    }
}
