using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Office.Tools;
using PlaceKit.Interfaces;
using PlaceKit.Tools;
using PlaceKit.ViewModels;
using PlaceKit.Views;
using Office = Microsoft.Office.Core;
using Word = Microsoft.Office.Interop.Word;

namespace PlaceKit.Services
{
    public sealed class TaskPaneService : ITaskPaneService
    {
        const int PaneWidth = 380;
        const double DefaultDpi = 96;

        readonly Func<CustomTaskPaneCollection> _taskPanes;
        readonly Dictionary<EditorViewModel, CustomTaskPane> _panes = new Dictionary<EditorViewModel, CustomTaskPane>();

        public TaskPaneService(Func<CustomTaskPaneCollection> taskPanes)
        {
            _taskPanes = taskPanes ?? throw new ArgumentNullException(nameof(taskPanes));
        }

        // A document can have several windows (View > New Window). The pane belongs to one of them and
        // goes when that window closes, so it is made again in the window the user works in.
        public void Show(EditorViewModel editor, Word.Window window)
        {
            if (_panes.TryGetValue(editor, out CustomTaskPane pane))
            {
                if (TryShowIn(pane, window))
                {
                    return;
                }

                Remove(editor);
            }

            pane = _taskPanes().Add(new EditorPaneControl(editor), editor.Title, window);
            pane.DockPosition = Office.MsoCTPDockPosition.msoCTPDockPositionRight;
            pane.DockPositionRestrict = Office.MsoCTPDockPositionRestrict.msoCTPDockPositionRestrictNoHorizontal;
            pane.Visible = true;
            pane.Width = (int)Math.Round(PaneWidth * GetDpi(window) / DefaultDpi);
            _panes.Add(editor, pane);
        }

        public void Hide(EditorViewModel editor)
        {
            if (_panes.TryGetValue(editor, out CustomTaskPane pane))
            {
                try
                {
                    pane.Visible = false;
                }
                catch (Exception ex) when (IsGone(ex))
                {
                    Remove(editor);
                }
            }
        }

        public void Remove(EditorViewModel editor)
        {
            if (!_panes.TryGetValue(editor, out CustomTaskPane pane))
            {
                return;
            }

            _panes.Remove(editor);

            // Removing disposes the pane's control, which may still be handling the click that closed the editor.
            try
            {
                pane.Visible = false;
                pane.Control.BeginInvoke(new Action(() => RemovePane(pane)));
            }
            catch (Exception ex) when (IsGone(ex))
            {
                RemovePane(pane);
            }
        }

        // What Office and VSTO throw for a pane deleted together with its window
        static bool IsGone(Exception ex) => ex is COMException || ex is ObjectDisposedException || ex is InvalidOperationException;

        // false if the pane is in another window or was deleted with its own (its Window is null then)
        static bool TryShowIn(CustomTaskPane pane, Word.Window window)
        {
            try
            {
                if (!(pane.Window is Word.Window paneWindow) || paneWindow.Hwnd != window.Hwnd)
                {
                    return false;
                }

                pane.Visible = true;
                return true;
            }
            catch (Exception ex) when (IsGone(ex))
            {
                return false;
            }
        }

        void RemovePane(CustomTaskPane pane)
        {
            try
            {
                _taskPanes().Remove(pane);
            }
            catch (Exception ex) when (IsGone(ex))
            {
                // Already gone with its document window.
            }
        }

        static double GetDpi(Word.Window window)
        {
            try
            {
                uint dpi = NativeMethods.GetDpiForWindow(new IntPtr(window.Hwnd));
                return dpi > 0 ? dpi : DefaultDpi;
            }
            catch (EntryPointNotFoundException)
            {
                return DefaultDpi;
            }
        }
    }
}
