using Armls.TreeSitter;

namespace Armls.Buffer;

/// <summary>
/// Represents (roughly) a file on the filesystem or a file open in
/// the editor. In our case, Buffer will always point to a JSON file.
/// </summary>
public class Buffer
{
    /// <summary>
    /// Text of the buffer.
    /// </summary>
    public string Text;

    /// <summary>
    /// A concrete syntax tree (<see cref="TSTree" />) constructed by
    /// TreeSitter for the <see cref="Text" />.
    /// </summary>
    public TSTree ConcreteTree;

    /// <summary>
    /// Creates an instance of <see cref="Buffer" /> with the given
    /// text and concrete syntax tree.
    /// </summary>
    public Buffer(string text, TSTree tree)
    {
        Text = text;
        ConcreteTree = tree;
    }

    /// <summary>
    /// Gets the string value associated with given `key` in the buffer
    /// JSON.
    /// </summary>
    public string? GetStringValue(string key)
    {
        var query = new TSQuery(
            @"(pair (string (string_content) @key) (string (string_content) @value))",
            TSJsonLanguage.Language()
        );
        var cursor = query.Execute(ConcreteTree.RootNode());

        while (cursor.Next(out TSQueryMatch? match))
        {
            var captures = match!.Captures();
            if (captures[0].Text(Text).Equals(key))
            {
                return captures[1].Text(Text);
            }
        }

        return null;
    }
}
