using System;
using System.Windows;
using System.Windows.Media;

namespace PlaceKit.Tools
{
    internal static class WindowPlacement
    {
        // No owner on purpose: an owner on another thread shares its input queue,
        // so the busy Word window would freeze this one.
        public static void CenterOver(Window window, IntPtr target)
        {
            if (target == IntPtr.Zero || !NativeMethods.GetWindowRect(target, out NativeMethods.Rect bounds))
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                return;
            }

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.SourceInitialized += (sender, e) =>
            {
                Matrix fromDevice = PresentationSource.FromVisual(window)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
                Point topLeft = fromDevice.Transform(new Point(bounds.Left, bounds.Top));
                Point bottomRight = fromDevice.Transform(new Point(bounds.Right, bounds.Bottom));
                window.Left = (topLeft.X + bottomRight.X - window.Width) / 2;
                window.Top = (topLeft.Y + bottomRight.Y - window.Height) / 2;
            };
        }
    }
}
