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

        PlaceholderOperationResult ClearPlaceholders(Word.Document document, PlaceholderPattern pattern, IProgress<ProgressInfo> progress, CancellationToken token);

        bool SelectPlaceholder(Word.Document document, string name, int occurrence);
    }
}
