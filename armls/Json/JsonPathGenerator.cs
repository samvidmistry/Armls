using Armls.Buffer;
using Armls.TreeSitter;
using System.Collections.Generic;

namespace Armls.Json
{
    /// <summary>
    /// A static utility class to generate a JSON path from a Tree-sitter node.
    /// </summary>
    public static class JsonPathGenerator
    {
        /// <summary>
        /// Traverses up the syntax tree from a starting node to construct a JSON path.
        /// The path is returned as a list of segments, e.g., ["resources", "0", "properties", "location"].
        /// </summary>
        /// <param name="buffer">The document buffer containing the source text, used to get key names.</param>
        /// <param name="startNode">The node to start traversal from.</param>
        /// <param name="bufferText">Text of the buffer which startNode is part of.</param>
        /// <returns>A list of strings representing the JSON path.</returns>
        public static List<string> FromNode(Buffer.Buffer buffer, TSNode startNode, string bufferText)
        {
            var path = new List<string>();
            var currentNode = startNode;

            // To determine the full path of a node, we must walk *up* the tree from it.
            // At each level, we check if the parent is a key-value pair or an array
            // to determine whether to add a key name or an index to our path.
            while ((currentNode = currentNode.Parent()) != null)
            {
                if (currentNode.Type == "pair")
                {
                    var keyNode = currentNode.ChildByFieldName("key");
                    if (keyNode != null)
                    {
                        // Keys are prepended because we are building the path from leaf to root.
                        path.Insert(0, keyNode.Text(buffer.Text).Trim('"'));
                    }
                }
                else if (currentNode.Type == "array")
                {
                    bool found = false;
                    for (uint i = 0; i < currentNode.NamedChildCount; i++)
                    {
                        if (currentNode.NamedChild(i).DescendantForPointRange(startNode.Start, startNode.End).NodeEquals(startNode))
                        {
                            path.Insert(0, i.ToString());
                            found = true;
                            break;
                        }
                    }
                    if (found) continue;
                    
                    throw new InvalidOperationException("StartNode not a child of any array element.");
                }
                else if (currentNode.Type == "object" && currentNode.Keys().Select(n => n.Text(bufferText)).Contains("apiVersion"))
                {
                    // We are at a top-level resource declaration in the template
                    for (uint i = 0; i < currentNode.NamedChildCount; i++)
                    {
                        var childNodeText = currentNode.NamedChild(i).ChildByFieldName("key")?.Text(bufferText).Trim('"') ?? "";
                        if (childNodeText == "type")
                        {
                            path.Insert(0, currentNode.NamedChild(i).ChildByFieldName("value")?.Text(bufferText).Trim('"') ??
                                           throw new InvalidOperationException("Value node not found for \"type\" key."));
                            break;
                        }
                    }
                }
            }

            return path;
        }
    }
}
