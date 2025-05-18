using System.Runtime.InteropServices;
using System.Text;

namespace Armls.TreeSitter;

internal static class Utils
{
    internal static (IntPtr, uint) GetUnmanagedUTF8String(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        var nativeText = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, nativeText, bytes.Length);
        Marshal.WriteByte(nativeText, bytes.Length, 0);

        return (nativeText, (uint)bytes.Length);
    }
}
