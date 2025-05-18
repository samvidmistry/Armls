namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;

public class TSQueryMatch
{
    internal readonly TSQueryMatchNative match;

    internal TSQueryMatch(IntPtr queryMatchPtr)
    {
        match = Marshal.PtrToStructure<TSQueryMatchNative>(queryMatchPtr);
    }

    internal TSQueryMatch(TSQueryMatchNative nativeMatch)
    {
        match = nativeMatch;
    }

    public ICollection<TSNode> Captures()
    {
        var capturesList = new List<TSNode>();
        var count = match.capture_count;
        var capturesPtr = match.captures;

        int size = Marshal.SizeOf<TSQueryCaptureNative>();
        for (int i = 0; i < count; i++)
        {
            var capturePtr = IntPtr.Add(capturesPtr, i * size);
            var nativeCapture = Marshal.PtrToStructure<TSQueryCaptureNative>(capturePtr);
            capturesList.Add(new TSNode(nativeCapture.node));
        }

        return capturesList;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct TSQueryMatchNative
{
    public uint id;
    public ushort pattern_index;
    public ushort capture_count;
    public IntPtr captures; // Pointer to TSQueryCapture array
}

[StructLayout(LayoutKind.Sequential)]
internal struct TSQueryCaptureNative
{
    public TSNodeNative node;
    public uint index;
}
