using System;

namespace PlaceKit.Models
{
    // The message is shown to the user as is.
    public sealed class AiServiceException : Exception
    {
        public AiServiceException(string message)
            : base(message)
        {
        }

        public AiServiceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
