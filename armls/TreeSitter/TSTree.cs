namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;

public class TSTree
{
    IntPtr tree;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_tree_print_dot_graph(IntPtr tree, int file_descriptor);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_tree_root_node(IntPtr tree);

    public TSTree(IntPtr tree)
    {
        this.tree = tree;
    }

    public TSNode RootNode()
    {
        return new TSNode(ts_tree_root_node(tree));
    }

    public void PrintDotGraph(string filename)
    {
        var fs = new FileStream(filename, FileMode.Create);
        ts_tree_print_dot_graph(tree, fs.SafeFileHandle.DangerousGetHandle().ToInt32());
    }
}
