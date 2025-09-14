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

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern uint ts_node_start_byte(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern uint ts_node_end_byte(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_node_descendant_for_point_range(
        TSNodeNative self,
        TSPoint start,
        TSPoint end
    );

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_node_named_descendant_for_point_range(
        TSNodeNative self,
        TSPoint start,
        TSPoint end
    );

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_node_type(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_node_child_by_field_name(
        TSNodeNative self,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string fieldName,
        uint fieldNameLength
    );

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern uint ts_node_named_child_count(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_node_named_child(TSNodeNative node, uint index);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSNodeNative ts_node_parent(TSNodeNative node);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern TSTreeCursorNative ts_tree_cursor_new(TSNodeNative node);

    public TSNode? Parent()
    {
        var parentNode = ts_node_parent(node);
        if (parentNode.id == IntPtr.Zero)
        {
            return null;
        }
        return new TSNode(parentNode);
    }

    public string Type => Marshal.PtrToStringUTF8(ts_node_type(node));

    public uint NamedChildCount => ts_node_named_child_count(node);

    public TSNode? ChildByFieldName(string fieldName)
    {
        var childNode = ts_node_child_by_field_name(node, fieldName, (uint)fieldName.Length);
        if (childNode.id == IntPtr.Zero)
        {
            return null;
        }
        return new TSNode(childNode);
    }

    public TSNode NamedChild(uint index)
    {
        return new TSNode(ts_node_named_child(node, index));
    }

    // Ideally I shouldn't be adding LSP specific knowledge
    // to TreeSitter package. But this one is just too convenient.
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

    /// <summary>
    /// Just like <see cref="DescendantForPointRange(TSPoint,
    /// TSPoint)" /> except that it takes in a single point and uses
    /// it as both the start and the end point of the range.
    /// </summary>
    public TSNode DescendantForPoint(TSPoint point)
    {
        return new TSNode(ts_node_descendant_for_point_range(this.node, point, point));
    }

    /// <summary>
    /// Get the smallest node within this node that spans the given range of bytes
    /// or (row, column) positions.
    /// </summary>
    public TSNode DescendantForPointRange(TSPoint start, TSPoint end)
    {
        return new TSNode(ts_node_descendant_for_point_range(this.node, start, end));
    }

    /// <summary>
    /// Get the smallest named node within this node that spans the given range of bytes
    /// or (row, column) positions.
    /// </summary>
    public TSNode NamedDescendantForPointRange(TSPoint start, TSPoint end)
    {
        return new TSNode(ts_node_named_descendant_for_point_range(this.node, start, end));
    }

    /// <summary>
    /// Walks the tree up to find a parent with given fieldName
    /// or null if no such parent exists.
    /// </summary>
    public TSNode? NamedParent(string fieldName)
    {
        using var cursor = Cursor();
        while (cursor.GoToParent())
        {
            if (cursor.CurrentFieldName() == fieldName)
            {
                return cursor.CurrentNode();
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all the keys from all the pairs defined under this node.
    /// </summary>
    public IEnumerable<TSNode> Keys()
    {
	List<TSNode> keys = new();
	var cursor = new TSQuery(@"(pair (string (string_content) @key) (_))", TSJsonLanguage.Language()).Execute(this);
	while (cursor.Next(out var queryMatch))
	{
	    keys.AddRange(queryMatch?.Captures() ?? []);
	}

	return keys;
    }

    /// <summary>
    /// Get the text associated with this node in the `sourceText`.
    /// </summary>
    public string Text(string sourceText)
    {
        var startByte = ts_node_start_byte(node);
        var length = (int)(ts_node_end_byte(node) - startByte);

        return System.Text.Encoding.UTF8.GetString(
            System.Text.Encoding.UTF8.GetBytes(sourceText),
            (int)startByte,
            length
        );
    }

    public TSTreeCursor Cursor()
    {
        return new TSTreeCursor(ts_tree_cursor_new(node));
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
