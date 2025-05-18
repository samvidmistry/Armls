namespace Armls.TreeSitter;

using System;
using System.Runtime.InteropServices;
using System.Text;

public class TSParser
{
    private IntPtr parser;

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_parser_new();

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern void ts_parser_delete(IntPtr parser);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern bool ts_parser_set_language(IntPtr parser, IntPtr language);

    [DllImport(
        "/Users/samvidmistry/projects/lsp/armls/tree-sitter/libtree-sitter.dylib",
        CallingConvention = CallingConvention.Cdecl
    )]
    private static extern IntPtr ts_parser_parse_string_encoding(
        IntPtr parser,
        IntPtr oldTree,
        IntPtr str,
        uint len,
        TSInputEncoding encoding
    );

    private enum TSInputEncoding
    {
        TSInputEncodingUTF8,
        TSInputEncodingUTF16,
    }

    public TSParser(IntPtr language)
    {
        parser = ts_parser_new();
        if (parser == IntPtr.Zero)
        {
            throw new Exception("Failed to create parser");
        }

        bool success = ts_parser_set_language(parser, language);
        if (!success)
        {
            ts_parser_delete(parser);
            throw new Exception("Failed to set language on parser");
        }
    }

    public TSTree ParseString(string text)
    {
        var (nativeText, length) = Utils.GetUnmanagedUTF8String(text);
        return new TSTree(
            ts_parser_parse_string_encoding(
                parser,
                IntPtr.Zero,
                nativeText,
                length,
                TSInputEncoding.TSInputEncodingUTF8
            )
        );
    }

    ~TSParser()
    {
        if (parser != IntPtr.Zero)
        {
            ts_parser_delete(parser);
            parser = IntPtr.Zero;
        }
    }
}
