using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Schema;

namespace Armls.Schema
{
    /// <summary>
    /// A static utility class to navigate a JSchema object using a JSON path.
    /// </summary>
    public static class SchemaNavigator
    {
        /// <summary>
        /// Finds a sub-schema within a root schema based on a path.
        /// </summary>
        /// <param name="rootSchema">The root JSchema to search within.</param>
        /// <param name="path">The path segments to follow, e.g., ["resources", "0", "properties", "location"].</param>
        /// <returns>The resolved JSchema if found; otherwise, null.</returns>
        public static JSchema? FindSchemaByPath(JSchema rootSchema, List<string> path)
        {
            JSchema? currentSchema = rootSchema;

            for (int i = 0; i < path.Count(); i++)
            {
                var segment = path[i];
                if (currentSchema == null)
                    return null;

                // Schemas can use combiners like anyOf/allOf. We will explore all paths
                // and get the first matching field description for simplicity
                IList<JSchema> combinator = null;
                if (currentSchema.AnyOf.Count > 0)
                {
                    combinator = currentSchema.AnyOf;
                }
                else if (currentSchema.AllOf.Count > 0)
                {
                    combinator = currentSchema.AllOf;
                }
                else if (currentSchema.OneOf.Count > 0)
                {
                    combinator = currentSchema.OneOf;
                }

                if (combinator is not null)
                {
                    foreach (var schemaPath in combinator)
                    {
                        var nestedSchema = FindSchemaByPath(schemaPath, path.Skip(i).ToList());
                        if (nestedSchema is not null)
                        {
                            return nestedSchema;
                        }
                    }

                    return null; // Path segment not found in any of the choices
                }

                // Attempt to navigate into an object property.
                if (currentSchema.Properties.TryGetValue(segment, out var propertySchema))
                {
                    currentSchema = propertySchema;
                }
                // If the segment is an integer, attempt to navigate into an array.
                else if (currentSchema.Type == JSchemaType.Array && int.TryParse(segment, out _))
                {
                    // For ARM templates, arrays usually have a single schema definition for all their items.
                    if (currentSchema.Items.Count > 0)
                    {
                        currentSchema = currentSchema.Items[0];
                    }
                    else
                    {
                        return null; // Array schema has no item definition.
                    }
                }
                else
                {
                    return null; // Path segment not found.
                }
            }

            return currentSchema;
        }
    }
}
