namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;

public static class TSJsonLanguage
{
    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter-grmmars/json.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr tree_sitter_json();

    public static IntPtr Language()
    {
        return tree_sitter_json();
    }
}
