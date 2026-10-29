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

        public void Show(EditorViewModel editor, Word.Window window)
        {
            if (_panes.TryGetValue(editor, out CustomTaskPane pane))
            {
                pane.Visible = true;
                return;
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
                pane.Visible = false;
            }
        }

        public void Remove(EditorViewModel editor)
        {
            if (!_panes.TryGetValue(editor, out CustomTaskPane pane))
            {
                return;
            }

            _panes.Remove(editor);
            try
            {
                _taskPanes().Remove(pane);
            }
            catch (Exception ex) when (ex is COMException || ex is ObjectDisposedException)
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
