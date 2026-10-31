using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    public sealed class UpdateService : IUpdateService
    {
        readonly IReleaseSource _releases;
        readonly ISettingsService _settings;
        readonly IProgressService _progress;
        readonly ProductInfo _product;
        readonly SynchronizationContext _wordThread;

        public UpdateService(IReleaseSource releases, ISettingsService settings, IProgressService progress, ProductInfo product, SynchronizationContext wordThread)
        {
            _releases = releases;
            _settings = settings;
            _progress = progress;
            _product = product;
            _wordThread = wordThread;
        }

        public ReleaseInfo AvailableUpdate { get; private set; }

        public event EventHandler AvailableUpdateChanged;

        // Word's thread is blocked while the progress window runs, so the request must not need it.
        public ReleaseInfo Check()
        {
            ReleaseInfo latest = _progress.Run("Checking for updates",
                (progress, token) => Task.Run(() => _releases.GetLatestAsync(token), token).GetAwaiter().GetResult());
            Remember(latest);
            return AvailableUpdate;
        }

        public async Task CheckInBackgroundAsync()
        {
            if (!_settings.Load().CheckForUpdates)
            {
                return;
            }

            try
            {
                // On the thread pool, since even starting a request can wait for the proxy to be found.
                ReleaseInfo latest = await Task.Run(() => _releases.GetLatestAsync(CancellationToken.None)).ConfigureAwait(false);
                _wordThread.Post(_ => Remember(latest), null);
            }
            catch (Exception ex)
            {
                // Nobody asked for this check, so a failure only goes to the trace.
                Trace.TraceWarning("PlaceKit: the update check failed: {0}", ex.Message);
            }
        }

        void Remember(ReleaseInfo latest)
        {
            AvailableUpdate = latest.IsNewerThan(_product.Version) ? latest : null;
            AvailableUpdateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
