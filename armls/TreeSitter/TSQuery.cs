namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;

public class TSQuery
{
    private IntPtr query;
    private IntPtr queryString;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_query_new(
        IntPtr language,
        IntPtr source,
        uint length,
        out uint errorOffset,
        out int errorType
    );

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_query_delete(IntPtr query);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_query_cursor_new();

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_query_cursor_exec(IntPtr cursor, IntPtr query, TSNodeNative node);

    public TSQuery(string queryString, IntPtr language)
    {
        var (nativeQuery, length) = Utils.GetUnmanagedUTF8String(queryString);
        this.queryString = nativeQuery;
        uint errorOffset;
        int errorType;
        query = ts_query_new(language, nativeQuery, length, out errorOffset, out errorType);
        if (query == IntPtr.Zero)
        {
            Marshal.FreeHGlobal(this.queryString);
            this.queryString = IntPtr.Zero;
            throw new Exception(
                $"Failed to create TSQuery. Error at offset {errorOffset}, type {errorType}"
            );
        }
    }

    public TSQueryCursor Execute(TSNode node)
    {
        var cursor = new TSQueryCursor(ts_query_cursor_new());
        ts_query_cursor_exec(cursor.cursor, query, node.node);
        return cursor;
    }

    ~TSQuery()
    {
        if (query != IntPtr.Zero)
        {
            ts_query_delete(query);
            query = IntPtr.Zero;
        }

        if (queryString != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(queryString);
            queryString = IntPtr.Zero;
        }
    }
}
