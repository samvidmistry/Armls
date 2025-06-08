using Armls.TreeSitter;

namespace Armls.Buffer;

/// <summary>
/// Represents (roughly) a file on the filesystem or a file open in
/// the editor.
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
}
