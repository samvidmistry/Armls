using System.Runtime.InteropServices;

namespace Armls.TreeSitter;

public class TSQueryCursor
{
    internal readonly IntPtr cursor;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern bool ts_query_cursor_next_capture(
        IntPtr cursor,
        ref TSQueryMatchNative match,
        out uint capture_index
    );

    internal TSQueryCursor(IntPtr cursor)
    {
        this.cursor = cursor;
    }

    public bool Next(out TSQueryMatch? match)
    {
        TSQueryMatchNative matchNative = new();
        uint captureIndex = 0;
        if (ts_query_cursor_next_capture(cursor, ref matchNative, out captureIndex))
        {
            match = new TSQueryMatch(matchNative);
            return match.match.capture_count > 0;
        }

        match = null;
        return false;
    }
}
