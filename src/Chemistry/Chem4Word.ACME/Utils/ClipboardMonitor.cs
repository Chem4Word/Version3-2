﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Chem4Word.ACME.Utils
{
    public sealed class ClipboardMonitor : IDisposable
    {
        private static class NativeMethods
        {
            /// <summary>
            /// Places the given window in the system-maintained clipboard format listener list.
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            /// <summary>
            /// Removes the given window from the system-maintained clipboard format listener list.
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

            /// <summary>
            /// Sent when the contents of the clipboard have changed.
            /// </summary>
            public const int WM_CLIPBOARDUPDATE = 0x031D;

            /// <summary>
            /// To find message-only windows, specify HWND_MESSAGE in the hwndParent parameter of the FindWindowEx function.
            /// </summary>
            public static IntPtr HWND_MESSAGE = new IntPtr(-3);
        }

        private HwndSource hwndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, NativeMethods.HWND_MESSAGE);

        public ClipboardMonitor()
        {
            hwndSource.AddHook(WndProc);
            NativeMethods.AddClipboardFormatListener(hwndSource.Handle);
        }

        public void Dispose()
        {
            NativeMethods.RemoveClipboardFormatListener(hwndSource.Handle);
            hwndSource.RemoveHook(WndProc);
            hwndSource.Dispose();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardContentChanged?.Invoke(this, EventArgs.Empty);
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Occurs when the clipboard content changes.
        /// </summary>
        public event EventHandler OnClipboardContentChanged;
    }
}