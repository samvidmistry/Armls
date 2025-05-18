using System.Runtime.InteropServices;

namespace Armls.TreeSitter
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TSPoint
    {
        public uint row;
        public uint column;
    }
}
