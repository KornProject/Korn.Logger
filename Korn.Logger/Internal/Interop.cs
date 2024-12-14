using System.Runtime.InteropServices;

namespace Korn.Logger.Internal;
internal static class Interop
{
    const string user = "user32";

    [DllImport(user)] public static extern 
        int MessageBox(nint windowHandle, string text, string caption, uint type);

    public static void MessageBox(string text, string caption) => MessageBox(0, text, caption, 0);
    public static void MessageBox(string text) => MessageBox(0, text, "MessageBox", 0);
}