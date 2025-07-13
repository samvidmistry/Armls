using System.Runtime.InteropServices;

namespace Armls.TreeSitter;

public class TSTreeCursor : IDisposable
{
    private TSTreeCursorNative cursor;
    private bool disposed;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_tree_cursor_delete(TSTreeCursorNative cursor);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_tree_cursor_current_node(ref TSTreeCursorNative cursor);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_tree_cursor_current_field_name(ref TSTreeCursorNative self);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern bool ts_tree_cursor_goto_parent(ref TSTreeCursorNative self);

    internal TSTreeCursor(TSTreeCursorNative cursor)
    {
        this.cursor = cursor;
        this.disposed = false;
    }

    public TSNode CurrentNode()
    {
        return new TSNode(ts_tree_cursor_current_node(ref cursor));
    }

    public string? CurrentFieldName()
    {
        return Marshal.PtrToStringUTF8(ts_tree_cursor_current_field_name(ref cursor));
    }

    public bool GoToParent()
    {
        return ts_tree_cursor_goto_parent(ref cursor);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            ts_tree_cursor_delete(cursor);
            disposed = true;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct TSTreeCursorNative
{
    IntPtr tree;
    IntPtr id;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public uint[] context;
}
