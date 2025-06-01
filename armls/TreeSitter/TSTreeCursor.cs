using System.Runtime.InteropServices;

namespace Armls.TreeSitter;

public class TSTreeCursor : IDisposable
{
    private readonly TSTreeCursorNative cursor;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_tree_cursor_delete(TSTreeCursorNative cursor);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_tree_cursor_current_node(ref const TSTreeCursorNative cursor);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_tree_cursor_current_field_name(const TSTreeCursor *self);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern bool ts_tree_cursor_goto_parent(TSTreeCursor *self);

    internal TSTreeCursor(TSTreeCursorNative cursor)
    {
        this.cursor = cursor;
    }

    public TSNode CurrentNode()
    {
	return TSNode(ts_tree_cursor_current_node(cursor));
    }

    public string CurrentFieldName()
    {
	return Marshal.PtrToStringUTF8(ts_tree_cursor_current_field_name(cursor));
    }

    public bool GoToParent()
    {
	ts_tree_cursor_goto_parent(cursor);
    }

    public void Dispose()
    {
        if (cursor is not null)
        {
            ts_tree_cursor_delete(cursor);
            cursor = null;
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
