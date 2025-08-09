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
        /// <returns>A list of strings representing the JSON path.</returns>
        public static List<string> FromNode(Buffer.Buffer buffer, TSNode startNode)
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
                    uint index = 0;
                    for (uint i = 0; i < currentNode.NamedChildCount; i++)
                    {
                        if (currentNode.NamedChild(i).Equals(startNode))
                        {
                            index = i;
                            break;
                        }
                    }
                    path.Insert(0, index.ToString());
                }
            }

            return path;
        }
    }
}
