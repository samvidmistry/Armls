using Armls.TreeSitter;

namespace Armls.Buffer;

public class Buffer
{
    public string Text;
    public TSTree ConcreteTree;

    public Buffer(string text, TSTree tree)
    {
        Text = text;
        ConcreteTree = tree;
    }
}
