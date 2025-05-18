namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;

public class TSNode
{
    internal readonly TSNodeNative node;

    internal TSNode(TSNodeNative nativeNode)
    {
        node = nativeNode;
    }

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSPoint ts_node_start_point(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSPoint ts_node_end_point(TSNodeNative node);

    // Ideally I shouldn't be adding LSP specific knowledge
    // to TreeSitter package. But this one is just too convinient.
    public OmniSharp.Extensions.LanguageServer.Protocol.Models.Range GetRange()
    {
        var start = ts_node_start_point(node);
        var end = ts_node_end_point(node);

        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(
                (int)start.row,
                (int)start.column
            ),
            new OmniSharp.Extensions.LanguageServer.Protocol.Models.Position(
                (int)end.row,
                (int)end.column
            )
        );
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct TSNodeNative
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public uint[] context;
    public IntPtr id;
    public IntPtr tree;
}
