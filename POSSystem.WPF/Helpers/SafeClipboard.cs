using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace POSSystem.WPF.Helpers
{
    /// <summary>
    /// مساعد آمن للتعامل مع الحافظة (Clipboard) في ويندوز لمنع خطأ
    /// OpenClipboard Failed (0x800401D0 / CLIPBRD_E_CANT_OPEN)
    /// </summary>
    public static class SafeClipboard
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static bool SetText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            // 1. Try Win32 native clipboard with retries first (avoids WPF OleSetClipboard flush bug)
            for (int i = 0; i < 10; i++)
            {
                if (TrySetWin32ClipboardText(text))
                    return true;
                Thread.Sleep(20);
            }

            // 2. Fallback to WPF SetDataObject(..., copy: false) with retries
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, false);
                    return true;
                }
                catch
                {
                    Thread.Sleep(25);
                }
            }

            return false;
        }

        private static bool TrySetWin32ClipboardText(string text)
        {
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                    return false;

                try
                {
                    EmptyClipboard();
                    int bytes = (text.Length + 1) * 2;
                    IntPtr hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                    if (hGlobal == IntPtr.Zero)
                        return false;

                    IntPtr target = GlobalLock(hGlobal);
                    if (target == IntPtr.Zero)
                        return false;

                    try
                    {
                        Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                        Marshal.WriteInt16(target, text.Length * 2, 0);
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }

                    if (SetClipboardData(CF_UNICODETEXT, hGlobal) != IntPtr.Zero)
                    {
                        return true;
                    }
                }
                finally
                {
                    CloseClipboard();
                }
            }
            catch
            {
                try { CloseClipboard(); } catch { }
            }
            return false;
        }
    }
}
