using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Armls.Schema;

/// <summary>
/// SchemaHandler is the singleton handler for all schema related
/// operations within Armls, such as validating a block of JSON
/// against a schema, looking up types and documentation information
/// about a field.
/// </summary>
public class SchemaHandler
{
    private readonly IDictionary<string, JSchema> schemaCache;
    private readonly AzureSchemaResolver schemaResolver;

    /// <summary>
    /// Creates an instance of <see cref="SchemaHandler" />.
    /// Keys for `localSchemas` are paths to schemas looking like
    /// `[/]schemas/2014-04-01/Microsoft.Sql.json`. Values are streams
    /// to the string definition of the schema for the keyed resource.
    /// </summary>
    public SchemaHandler(IDictionary<string, Stream> localSchemas)
    {
        schemaCache = new ConcurrentDictionary<string, JSchema>();
        schemaResolver = new AzureSchemaResolver(localSchemas);
    }

    /// <summary>
    /// Validates the `text` according to schema present at
    /// `schemaUrl`. It caches the schema if this is the first time
    /// the function is being called with this `schemaUrl`. This
    /// function assumes that `text` contains a valid json document.
    /// Passing an invalid json document will throw an exception.
    /// </summary>
    public async Task<IList<ValidationError>> ValidateAsync(string schemaUrl, string text)
    {
        IList<ValidationError> errors;
        var schema = await GetSchemaAsync(schemaUrl);
        JObject.Parse(text).IsValid(schema, out errors);

        return errors;
    }

    /// <summary>
    /// Gets a <see cref="JSchema" /> for schema present at `schemaUrl`.
    /// </summary>
    private async Task<JSchema> GetSchemaAsync(string schemaUrl)
    {
        if (!schemaCache.ContainsKey(schemaUrl))
        {
            using var httpClient = new HttpClient();
            var schemaJson = await httpClient.GetStringAsync(schemaUrl);
            schemaCache[schemaUrl] = JSchema.Parse(schemaJson, schemaResolver);
        }

        return schemaCache[schemaUrl];
    }

    /// <summary>
    /// Clears the schemas stored in cache, making the handler
    /// download the schema again for next validation call.
    /// </summary>
    /// <remarks>
    /// Be careful not to cripple the performance of your application
    /// with this function.
    /// </remarks>
    public void ClearCache()
    {
        schemaCache.Clear();
    }
}
