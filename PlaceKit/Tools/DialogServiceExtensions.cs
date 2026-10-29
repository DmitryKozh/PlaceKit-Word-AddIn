using System;
using System.Diagnostics;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Tools
{
    public static class DialogServiceExtensions
    {
        // Exceptions must not reach Office: it swallows them in ribbon callbacks and
        // can crash when they escape a task pane.
        public static void RunAndReportErrors(this IDialogService dialogs, Action action)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                // canceled by the user, nothing to report
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlaceKit: {0}", ex);
                dialogs.ShowMessage("PlaceKit", $"Something went wrong: {ex.Message}", MessageKind.Error);
            }
        }
    }
}
