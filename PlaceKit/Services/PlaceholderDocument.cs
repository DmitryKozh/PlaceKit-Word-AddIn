using System.Collections.Generic;
using PlaceKit.Interfaces;
using PlaceKit.Models;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class PlaceholderDocument : IPlaceholderDocument
    {
        readonly Word.Document _document;
        readonly IPlaceholderService _placeholders;
        readonly IProgressService _progress;

        public PlaceholderDocument(Word.Document document, IPlaceholderService placeholders, IProgressService progress)
        {
            _document = document;
            _placeholders = placeholders;
            _progress = progress;
        }

        public IReadOnlyList<PlaceholderInfo> LoadPlaceholders() =>
            _progress.Run("Loading placeholders", (progress, token) => _placeholders.GetPlaceholders(_document, progress, token));

        public PlaceholderOperationResult SavePlaceholders(IReadOnlyDictionary<string, string> values) =>
            _progress.Run("Saving placeholders", (progress, token) => _placeholders.UpdatePlaceholders(_document, values, progress, token));

        public bool SelectPlaceholder(string name, int occurrence) => _placeholders.SelectPlaceholder(_document, name, occurrence);
    }
}
