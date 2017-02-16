﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rubberduck.VBEditor.SafeComWrappers.Abstract;
using Rubberduck.VBEditor.WindowsApi;

namespace Rubberduck.VBEditor.Events
{
    public static class VBEEvents
    {
        private static User32.WinEventProc _eventProc;
        private static IntPtr _eventHandle;
        private static IVBE _vbe;

        public struct WindowInfo
        {
            private readonly IntPtr _handle;
            private readonly IWindow _window;

            public IntPtr Hwnd { get { return _handle; } } 
            public IWindow Window { get { return _window; } }

            public WindowInfo(IntPtr handle, IWindow window)
            {
                _handle = handle;
                _window = window;                            
            }
        }

        //This *could* be a ConcurrentDictionary, but there other operations that need the lock around it anyway.
        private static readonly Dictionary<IntPtr, WindowInfo> TrackedWindows = new Dictionary<IntPtr, WindowInfo>();
        private static readonly object ThreadLock = new object();
        
        private static uint _threadId;

        public static void HookEvents(IVBE vbe)
        {
            _vbe = vbe;
            if (_eventHandle == IntPtr.Zero)
            {               
                _eventProc = VbeEventCallback;
                _threadId = User32.GetWindowThreadProcessId(new IntPtr(_vbe.MainWindow.HWnd), IntPtr.Zero);
                _eventHandle = User32.SetWinEventHook((uint)WinEvent.Min, (uint)WinEvent.Max, IntPtr.Zero, _eventProc, 0, _threadId, WinEventFlags.OutOfContext);
            }
        }

        public static void UnhookEvents()
        {
            User32.UnhookWinEvent(_eventHandle);
        }

        public static void VbeEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild,
            uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero && idObject == (int)ObjId.Caret && eventType == (uint)WinEvent.ObjectLocationChange && hwnd.ToWindowType() == WindowType.VbaWindow)
            {
                OnSelectionChanged(hwnd);             
            }
            else if (idObject == (int)ObjId.Window &&
                     (eventType == (uint)WinEvent.ObjectCreate || eventType == (uint)WinEvent.ObjectDestroy) && 
                     hwnd.ToWindowType() != WindowType.Indeterminate)
            {
                if (eventType == (uint) WinEvent.ObjectCreate)
                {
                    AttachWindow(hwnd);
                }
                else if (eventType == (uint)WinEvent.ObjectDestroy)
                {
                    DetachWindow(hwnd);
                }
            }
        }

        private static void AttachWindow(IntPtr hwnd)
        {
            lock (ThreadLock)
            {
                Debug.Assert(!TrackedWindows.ContainsKey(hwnd));
                var window = GetWindowFromHwnd(hwnd);
                if (window == null) return;
                var info = new WindowInfo(hwnd, window);
                TrackedWindows.Add(hwnd, info);
            }           
        }

        private static void DetachWindow(IntPtr hwnd)
        {
            lock (ThreadLock)
            {
                Debug.Assert(TrackedWindows.ContainsKey(hwnd));
                TrackedWindows.Remove(hwnd);
            }             
        }

        public static event EventHandler<SelectionChangedEventArgs> SelectionChanged;
        private static void OnSelectionChanged(IntPtr hwnd)
        {
            if (SelectionChanged != null)
            {
                var pane = GetCodePaneFromHwnd(hwnd);
                SelectionChanged.Invoke(_vbe, new SelectionChangedEventArgs(pane));
            }
        }

        //Pending location of a suitable event - might need a subclass here instead.
        public static event EventHandler<WindowChangedEventArgs> ForgroundWindowChanged;
        private static void OnForgroundWindowChanged(WindowInfo info)
        {
            if (ForgroundWindowChanged != null)
            {
                ForgroundWindowChanged.Invoke(_vbe, new WindowChangedEventArgs(info.Hwnd, info.Window));
            }
        } 

        private static ICodePane GetCodePaneFromHwnd(IntPtr hwnd)
        {
            var caption = hwnd.GetWindowText();
            return _vbe.CodePanes.FirstOrDefault(x => x.Window.Caption.Equals(caption));
        }

        private static IWindow GetWindowFromHwnd(IntPtr hwnd)
        {
            var caption = hwnd.GetWindowText();
            return _vbe.Windows.FirstOrDefault(x => x.Caption.Equals(caption));
        }

        /// <summary>
        /// A helper function that returns <c>true</c> when the specified handle is that of the foreground window.
        /// </summary>
        /// <returns>True if the active thread is on the VBE's thread.</returns>
        public static bool IsVbeWindowActive()
        {           
            uint hThread;
            User32.GetWindowThreadProcessId(User32.GetForegroundWindow(), out hThread);
            return (IntPtr)hThread == (IntPtr)_threadId;
        }

        public enum WindowType
        {
            Indeterminate,
            VbaWindow,
            DesignerWindow
        }

        public static WindowType ToWindowType(this IntPtr hwnd)
        {
            var name = new StringBuilder(128);
            User32.GetClassName(hwnd, name, name.Capacity);
            WindowType id;
            return Enum.TryParse(name.ToString(), out id) ? id : WindowType.Indeterminate;
        }
    }
}
