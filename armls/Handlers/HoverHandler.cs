using Armls.Buffer;
using Armls.TreeSitter;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Handlers;

public class HoverHandler : HoverHandlerBase
{
    private BufferManager bufManager;

    public HoverHandler(BufferManager manager)
    {
        bufManager = manager;
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        var buffer = bufManager.GetBuffer(request.TextDocument.Uri);

        if (buffer is null)
        {
            return Task.FromResult((Hover?)null);
        }

        var cursorPosition = new TSPoint()
        {
            row = (uint)request.Position.Line,
            column = (uint)request.Position.Character,
        };

        // Check which side of the expression we are on, key or value
        var rootNode = buffer.ConcreteTree.RootNode();
        var node = rootNode.DescendantForPointRange(cursorPosition, cursorPosition);
        var keyParent = node.NamedParent("key");

        if (keyParent is not null)
        {
            // we are on the key side
        }
        else
        {
            // we are on the value side
        }
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
