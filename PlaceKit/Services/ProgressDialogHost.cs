using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using PlaceKit.Models;
using PlaceKit.Tools;
using PlaceKit.ViewModels;
using PlaceKit.Views;

namespace PlaceKit.Services
{
    // The window gets its own UI thread: Word's thread is busy with the operation
    // and can't pump messages, so the window (and Cancel) would freeze.
    internal sealed class ProgressDialogHost : IProgress<ProgressInfo>, IDisposable
    {
        static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

        readonly ManualResetEventSlim _started = new ManualResetEventSlim();
        readonly Thread _thread;
        readonly string _title;
        readonly CancellationTokenSource _cancellation;
        readonly IntPtr _ownerWindow;
        readonly TimeSpan _showDelay;

        Dispatcher _dispatcher;
        DispatcherTimer _showTimer;
        ProgressWindow _window;
        ProgressViewModel _viewModel;
        ProgressInfo _latest;
        int _updateQueued;
        volatile bool _wasShown;

        public ProgressDialogHost(string title, CancellationTokenSource cancellation, IntPtr ownerWindow, TimeSpan showDelay)
        {
            _title = title;
            _cancellation = cancellation;
            _ownerWindow = ownerWindow;
            _showDelay = showDelay;

            _thread = new Thread(RunDispatcher) { IsBackground = true, Name = "PlaceKit progress" };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
            _started.Wait();
        }

        // Called from Word's thread. Only one update is queued at a time, so a fast loop
        // can't flood the dispatcher.
        public void Report(ProgressInfo value)
        {
            Volatile.Write(ref _latest, value);
            if (Interlocked.Exchange(ref _updateQueued, 1) == 0)
            {
                _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyLatest));
            }
        }

        public void Dispose()
        {
            _dispatcher.BeginInvoke(new Action(Close));
            _thread.Join(ShutdownTimeout);
            _started.Dispose();

            // The window took the focus, give it back to Word.
            if (_wasShown && _ownerWindow != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(_ownerWindow);
            }
        }

        void RunDispatcher()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _dispatcher.UnhandledException += OnUnhandledException;
            try
            {
                _viewModel = new ProgressViewModel(_title, _cancellation.Cancel);
                _window = new ProgressWindow { DataContext = _viewModel };
                WindowPlacement.CenterOver(_window, _ownerWindow);
                _showTimer = new DispatcherTimer(_showDelay, DispatcherPriority.Normal, OnShowTimerTick, _dispatcher);
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlaceKit: cannot create the progress window: {0}", ex);
                _window = null;
            }
            finally
            {
                _started.Set();
            }

            Dispatcher.Run();
        }

        void OnShowTimerTick(object sender, EventArgs e)
        {
            _showTimer.Stop();
            _wasShown = true;
            _window?.Show();
        }

        void ApplyLatest()
        {
            Interlocked.Exchange(ref _updateQueued, 0);
            _viewModel?.Update(Volatile.Read(ref _latest));
        }

        void Close()
        {
            _showTimer?.Stop();
            _window?.CloseFromHost();
            _dispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        }

        static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // An exception on this thread would take Word down.
            Trace.TraceError("PlaceKit: progress window error: {0}", e.Exception);
            e.Handled = true;
        }
    }
}
