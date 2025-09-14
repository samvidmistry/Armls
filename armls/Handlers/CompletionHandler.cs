using Armls.Buffer;
using Armls.Json;
using Armls.Schema;
using Armls.TreeSitter;
using Newtonsoft.Json.Schema;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Handlers;

/// <summary>
/// Handler responsible for textDocument/completion requests. It suggests available
/// object fields at the current cursor location using the minimal composed schema.
/// </summary>
public class CompletionHandler : CompletionHandlerBase
{
    private readonly BufferManager bufManager;
    private readonly MinimalSchemaComposer schemaComposer;

    public CompletionHandler(BufferManager manager, MinimalSchemaComposer schemaComposer)
    {
        bufManager = manager;
        this.schemaComposer = schemaComposer;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override Task<CompletionItem> Handle(
        CompletionItem request,
        CancellationToken cancellationToken
    )
    {
        // We are not enriching completion items on resolve yet.
        return Task.FromResult(request);
    }

    /// <summary>
    /// Returns a list of completion candidates available at the cursor position.
    /// </summary>
    public override async Task<CompletionList> Handle(
        CompletionParams request,
        CancellationToken cancellationToken
    )
    {
        // Simple algorithm for getting the completion candidates
        // 1. Get the minimal schema
        // 2. Navigate to cursor position in the schema
        // 3. Get applicable fields
        // 4. Remove already added fields

        var completionList = new CompletionList();
        
        var buffer = bufManager.GetBuffer(request.TextDocument.Uri);
        var schemaUrl = buffer?.GetStringValue("$schema");
        if (schemaUrl is null) return completionList;

        var schema = await schemaComposer.ComposeSchemaAsync(schemaUrl, buffer!.GetResourceTypes());
        if (schema is null) return completionList;

        var cursor = new TSPoint{
            row = (uint)request.Position.Line,
            column = (uint)request.Position.Character,
        };

	// Schema path contains the path /till/ the last element, which in our case is the field we are trying to write.
        // So we get the path only till the parent.
        var path = Json.JsonPathGenerator.FromNode(buffer, buffer.ConcreteTree.RootNode().DescendantForPoint(cursor).Parent());
        if (path is null || path.Count == 0) return completionList;
        
        var targetSchema = Schema.SchemaNavigator.FindSchemaByPath(schema, path);
        if (targetSchema is null) return completionList;

        return new CompletionList(FindCompletionCandidates(targetSchema).DistinctBy(c => c.Label + ":" + c.Documentation));
    }

    private IEnumerable<CompletionItem> FindCompletionCandidates(JSchema schema)
    {
	if (schema.AllOf.Count != 0 || schema.AnyOf.Count != 0 || schema.OneOf.Count != 0)
	{
	    return schema.AllOf.Concat(schema.AnyOf).Concat(schema.OneOf).SelectMany(childSchema => FindCompletionCandidates(childSchema));
	}

	return schema.Properties.Select(kvp => new CompletionItem()
	{
	    Label = kvp.Key,
	    Documentation = new StringOrMarkupContent(kvp.Value.Description ?? "")
	});
    }

    public override string? ToString()
    {
        return base.ToString();
    }

    protected override CompletionRegistrationOptions CreateRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
    {
        return new CompletionRegistrationOptions{
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.json", "**/*.jsonc"),
            TriggerCharacters = new string[] { "\"" },
            ResolveProvider = false
        };
    }
}
