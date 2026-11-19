using System;
using System.ComponentModel;
using System.Diagnostics;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    // Never throws: without a default browser, or with an installed file deleted, it says so instead.
    public sealed class UrlLauncher : IUrlLauncher
    {
        readonly IDialogService _dialogs;

        public UrlLauncher(IDialogService dialogs)
        {
            _dialogs = dialogs;
        }

        public void Open(Uri url)
        {
            string target = url.IsFile ? url.LocalPath : url.AbsoluteUri;
            Start(new ProcessStartInfo(target) { UseShellExecute = true }, target);
        }

        public void ShowInFolder(string path) => Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\""), path);

        void Start(ProcessStartInfo startInfo, string target)
        {
            try
            {
                Process.Start(startInfo)?.Dispose();
            }
            catch (Win32Exception ex)
            {
                Trace.TraceWarning("PlaceKit: cannot open {0}: {1}", target, ex.Message);
                _dialogs.ShowMessage("PlaceKit", $"Windows could not open {target}. {ex.Message}", MessageKind.Warning);
            }
        }
    }
}
