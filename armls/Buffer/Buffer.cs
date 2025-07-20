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
    /// Indicates whether this buffer has been changed since the
    /// last analysis and requires to be analyzed again.
    /// </summary>
    public bool RequiresAnalysis;

    /// <summary>
    /// Creates an instance of <see cref="Buffer" /> with the given
    /// text and concrete syntax tree.
    /// </summary>
    public Buffer(string text, TSTree tree)
    {
        Text = text;
        ConcreteTree = tree;
        RequiresAnalysis = true;
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

    /// <summary>
    /// Extracts all resource types and their API versions from the ARM template's resources array.
    /// Returns a dictionary mapping resource types to their API versions like {"Microsoft.Storage/storageAccounts": "2021-04-01"}.
    /// </summary>
    public Dictionary<string, string> GetResourceTypes()
    {
        var resourceTypesWithVersions = new Dictionary<string, string>();

        // Query to find all resource objects within resources array
        var query = new TSQuery(
            @"(pair (string (string_content) @key) (array (object) @resource))",
            TSJsonLanguage.Language()
        );
        var cursor = query.Execute(ConcreteTree.RootNode());

        while (cursor.Next(out TSQueryMatch? match))
        {
            var captures = match!.Captures();
            if (captures.Count >= 2 && captures[0].Text(Text).Equals("resources"))
            {
                var resourceNode = captures[1];

                var resourceType = GetPropertyValue(resourceNode, "type");
                var apiVersion = GetPropertyValue(resourceNode, "apiVersion");

                if (
                    !string.IsNullOrWhiteSpace(resourceType)
                    && !string.IsNullOrWhiteSpace(apiVersion)
                )
                {
                    resourceTypesWithVersions[resourceType] = apiVersion;
                }
            }
        }

        return resourceTypesWithVersions;
    }

    /// <summary>
    /// Helper method to extract a property value from a JSON object node.
    /// </summary>
    private string? GetPropertyValue(TSNode objectNode, string propertyName)
    {
        var propertyQuery = new TSQuery(
            @"(pair (string (string_content) @prop_key) (string (string_content) @prop_value))",
            TSJsonLanguage.Language()
        );
        var propertyCursor = propertyQuery.Execute(objectNode);

        while (propertyCursor.Next(out TSQueryMatch? propMatch))
        {
            var propCaptures = propMatch!.Captures();
            if (propCaptures.Count >= 2 && propCaptures[0].Text(Text).Equals(propertyName))
            {
                return propCaptures[1].Text(Text);
            }
        }

        return null;
    }
}
