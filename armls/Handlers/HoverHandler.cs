using Armls.Buffer;
using Armls.Schema;
using Armls.TreeSitter;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Handlers;

/// <summary>
/// Handler responsible for Hover calls from language client. For the
/// documentation of overridden methods, please refer to <see
/// href="https://github.com/OmniSharp/csharp-language-server-protocol">C#-LSP</see>
/// and <see
/// href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/">LSP
/// Spec</see>.
/// </summary>
public class HoverHandler : HoverHandlerBase
{
    private BufferManager bufManager;
    private MinimalSchemaComposer schemaComposer;

    public HoverHandler(BufferManager manager, MinimalSchemaComposer schemaComposer)
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

    public override async Task<Hover?> Handle(
        HoverParams request,
        CancellationToken cancellationToken
    )
    {
        var buffer = bufManager.GetBuffer(request.TextDocument.Uri);
        if (buffer is null)
            return null;

        var schemaUrl = buffer.GetStringValue("$schema");
        if (schemaUrl is null)
            return null;

        var schema = await schemaComposer.ComposeSchemaAsync(schemaUrl, buffer.GetResourceTypes());
        if (schema is null)
            return null;

        var cursorPosition = new TSPoint()
        {
            row = (uint)request.Position.Line,
            column = (uint)request.Position.Character,
        };

        var rootNode = buffer.ConcreteTree.RootNode();
        var hoveredNode = rootNode.DescendantForPointRange(cursorPosition, cursorPosition);
        if (hoveredNode is null)
            return null;

        var path = Json.JsonPathGenerator.FromNode(buffer, hoveredNode, buffer.Text);
        if (path.Count == 0)
            return null;

        var targetSchema = Schema.SchemaNavigator.FindSchemaByPath(schema, path);
        if (targetSchema?.Description is null)
            return null;

        return new Hover
        {
            Contents = new MarkedStringsOrMarkupContent(
                new MarkupContent { Kind = MarkupKind.Markdown, Value = targetSchema.Description }
            ),
            Range = hoveredNode.GetRange(),
        };
    }

    public override string? ToString()
    {
        return base.ToString();
    }

    protected override HoverRegistrationOptions CreateRegistrationOptions(
        HoverCapability capability,
        ClientCapabilities clientCapabilities
    )
    {
        return new HoverRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.json", "**/*.jsonc"),
        };
    }
}
