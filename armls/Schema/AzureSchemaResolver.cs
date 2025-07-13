using System.Text;
using Newtonsoft.Json.Schema;

namespace Armls.Schema;

/// <summary>
/// Resolves the URL to an ARM template schema to a `Stream` of schema
/// definition.
/// </summary>
internal class AzureSchemaResolver : JSchemaResolver
{
    private const string SCHEMA_BASE_URL = "https://schema.management.azure.com/";
    private const string STUB_SCHEMA = "{\"type\": \"object\", \"additionalProperties\": true}";

    private IDictionary<string, Stream> localSchemas;

    /// <summary>
    /// Creates an instance of <see cref="AzureSchemaResolver" />.
    /// Keys for `localSchemas` are paths to schemas looking like
    /// `[/]schemas/2014-04-01/Microsoft.Sql.json`. Values are streams
    /// to the string definition of the schema for the keyed resource.
    /// </summary>
    public AzureSchemaResolver(IDictionary<string, Stream> localSchemas)
    {
        // Map path names to URLs because the templates will have URLs
        this.localSchemas = localSchemas
            .Select(
                (kvp) => KeyValuePair.Create(SCHEMA_BASE_URL + kvp.Key.TrimStart('/'), kvp.Value)
            )
            .ToDictionary();
    }

    public override Stream? GetSchemaResource(
        ResolveSchemaContext context,
        SchemaReference reference
    )
    {
        var resolvedUri = reference.BaseUri ?? context.ResolverBaseUri;

        if (localSchemas.TryGetValue(resolvedUri?.ToString() ?? "", out Stream? val))
        {
            return val;
        }

        // TODO: Is this useful at all?
        return new MemoryStream(Encoding.UTF8.GetBytes(STUB_SCHEMA));
    }
}
