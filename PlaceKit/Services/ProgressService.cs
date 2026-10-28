using System;
using System.Threading;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    public sealed class ProgressService : IProgressService
    {
        static readonly TimeSpan ShowDelay = TimeSpan.FromMilliseconds(400);

        readonly IWordHost _wordHost;

        public ProgressService(IWordHost wordHost)
        {
            _wordHost = wordHost;
        }

        public T Run<T>(string title, Func<IProgress<ProgressInfo>, CancellationToken, T> operation)
        {
            using (CancellationTokenSource cancellation = new CancellationTokenSource())
            using (ProgressDialogHost dialog = new ProgressDialogHost(title, cancellation, _wordHost.MainWindowHandle, ShowDelay))
            {
                return operation(dialog, cancellation.Token);
            }
        }
    }
}
