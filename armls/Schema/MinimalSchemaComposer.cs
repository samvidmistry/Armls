using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace Armls.Schema;

/// <summary>
/// Composes minimal ARM template schemas by filtering the deploymentTemplate.json schema
/// to only reference the resource schemas actually used in the template.
/// This allows JSON.NET to download only 3-5 schemas instead of 1,300+.
/// </summary>
public class MinimalSchemaComposer
{
    private readonly Dictionary<string, string> schemaJsonCache;
    private readonly HttpClient httpClient;
    private readonly string schemaDirectory = "/Users/samvidmistry/Downloads/schemas";
    private readonly Dictionary<string, string> schemaIndex;

    public MinimalSchemaComposer()
    {
        schemaJsonCache = new();
        httpClient = new();
        var indexPath = Path.Combine(schemaDirectory, "schema_index.json");
        var indexJson = File.ReadAllText(indexPath);
        schemaIndex =
            JsonConvert.DeserializeObject<Dictionary<string, string>>(indexJson)
            ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// For non `deploymentTemplate` schemas, it downloads the schema
    /// off the internet and returns it unmodified. For
    /// `deploymentTemplate`, creates a minimal schema by downloading
    /// the full deploymentTemplate.json schema from the internet,
    /// filtering its resource references to only include detected
    /// resource types, and using a preloaded resolver with provider
    /// schemas loaded from local files.
    /// </summary>
    public async Task<JSchema?> ComposeSchemaAsync(
        string baseSchemaUrl,
        Dictionary<string, string> resourceTypesWithVersions
    )
    {
        if (!schemaJsonCache.TryGetValue(baseSchemaUrl, out var schemaJson))
        {
            schemaJson = await httpClient.GetStringAsync(baseSchemaUrl);
            schemaJsonCache[baseSchemaUrl] = schemaJson;
        }

        if (!baseSchemaUrl.Contains("deploymentTemplate.json"))
        {
            return JSchema.Load(
                new JsonTextReader(new StringReader(schemaJson)),
                new JSchemaUrlResolver()
            );
        }

        var resolver = new JSchemaPreloadedResolver();
        var resourceReferences = new JArray();
        var schemaUrls = new HashSet<string>
        {
            "https://schema.management.azure.com/schemas/common/definitions.json",
        };

        foreach (var (resourceType, apiVersion) in resourceTypesWithVersions)
        {
            var parts = resourceType.Split('/');
            if (parts.Length < 2)
                continue;
            var provider = parts[0];
            var resourceName = parts[1];
            var schemaUrl =
                $"https://schema.management.azure.com/schemas/{apiVersion}/{provider}.json";

            schemaUrls.Add(schemaUrl);
            resourceReferences.Add(
                new JObject { ["$ref"] = $"{schemaUrl}#/resourceDefinitions/{resourceName}" }
            );
        }

        (
            await Task.WhenAll(
                schemaUrls
                    .Where(url =>
                        schemaIndex.TryGetValue(url, out var filename)
                        && File.Exists(Path.Combine(schemaDirectory, filename))
                    )
                    .Select(async url => new
                    {
                        Url = new Uri(url),
                        Content = await File.ReadAllTextAsync(
                            Path.Combine(schemaDirectory, schemaIndex[url])
                        ),
                    })
            )
        )
            .ToList()
            .ForEach(s => resolver.Add(s.Url, s.Content));

        return ConstructSchemaWithResources(schemaJson, resourceReferences) is { } minimalSchemaJson
            ? JSchema.Load(new JsonTextReader(new StringReader(minimalSchemaJson)), resolver)
            : null;
    }

    /// <summary>
    /// Generates a minimal deploymentTemplate.json schema by:
    /// 1. Replacing the 1300+ references in branch 0 with only our detected resource types
    /// 2. Removing all other resource definition branches to keep the schema minimal.
    /// </summary>
    private string? ConstructSchemaWithResources(string baseSchemaJson, JArray resourceReferences)
    {
        try
        {
            var schemaObj = JObject.Parse(baseSchemaJson);

            if (
                schemaObj.SelectToken("definitions.resource.oneOf[0].allOf[1].oneOf")
                is JArray resourceRefsArray
            )
            {
                resourceRefsArray.Replace(resourceReferences);
            }

            if (
                schemaObj.SelectToken("definitions.resource.oneOf") is JArray oneOfArray
                && oneOfArray.Any()
            )
            {
                oneOfArray.ReplaceAll(oneOfArray.First());
            }

            return schemaObj.ToString();
        }
        catch (Exception)
        {
            // Return original schema if generation fails
            return null;
        }
    }
}
