using System.Collections.Generic;
using PlaceKit.Interfaces;

namespace PlaceKit.Services
{
    public sealed class ValueFinder : IValueFinder
    {
        readonly IAiService _ai;

        public ValueFinder(IAiService ai)
        {
            _ai = ai;
        }

        public IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names) =>
            ValueRequest.ParseValues(_ai.Send(ValueRequest.Create(text, names), "Finding the values"), names);
    }
}
