using System;
using System.Runtime.InteropServices;

namespace Korn.Logger.Internal
{
    internal static class Interop
    {
        const string user = "user32";

        [DllImport(user)] public static extern
            int MessageBox(IntPtr hwnd, string text, string caption, uint type);

        public static void MessageBox(string text, string caption) => MessageBox(IntPtr.Zero, text, caption, 0);
        public static void MessageBox(string text) => MessageBox(IntPtr.Zero, text, "MessageBox", 0);
    }
}