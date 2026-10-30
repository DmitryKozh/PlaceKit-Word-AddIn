using System;

namespace PlaceKit.Models
{
    // The message is shown to the user as it is.
    public sealed class ValueFinderException : Exception
    {
        public ValueFinderException(string message)
            : base(message)
        {
        }

        public ValueFinderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
