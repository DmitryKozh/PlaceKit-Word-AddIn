using System;
using System.Threading.Tasks;
using PlaceKit.Models;

namespace PlaceKit.Interfaces
{
    public interface IUpdateService
    {
        // A release newer than this version, once a check found one.
        ReleaseInfo AvailableUpdate { get; }

        // Raised on Word's thread.
        event EventHandler AvailableUpdateChanged;

        // Shows the progress window. Returns the newer release, or null when this is the latest version.
        // Throws UpdateCheckException, or OperationCanceledException when the user cancels.
        ReleaseInfo Check();

        // For Word's start, when Settings allow it. Never shows anything, failures included.
        Task CheckInBackgroundAsync();
    }
}
