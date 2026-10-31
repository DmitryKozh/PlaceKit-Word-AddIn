using System;
using System.Diagnostics;
using PlaceKit.Interfaces;

namespace PlaceKit.Services
{
    public sealed class UrlLauncher : IUrlLauncher
    {
        public void Open(Uri url) => Process.Start(new ProcessStartInfo(url.IsFile ? url.LocalPath : url.AbsoluteUri) { UseShellExecute = true });
    }
}
