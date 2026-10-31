using System;

namespace PlaceKit.Models
{
    // The message is shown to the user as is.
    public sealed class UpdateCheckException : Exception
    {
        public UpdateCheckException(string message)
            : base(message)
        {
        }

        public UpdateCheckException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
