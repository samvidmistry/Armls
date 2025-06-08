using Armls.Buffer;
using Armls.TreeSitter;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Armls.Handlers;

/// <summary>
/// This handler is responsible to receive all updates about changes
/// in the text published by the language client and update <see
/// cref="BufferManager" /> with the latest text, i.e., it keeps the
/// editor window in language client and the <see cref="Buffer" />
/// stored in <see cref="BufferManager" /> in sync. It is also
/// responsible to run analysis over entire project when any of the
/// files change and then publishing the diagnostics returned by the
/// <see cref="Analyzer" /> to language client. For the documentation
/// of overridden methods, please refer to <see
/// href="https://github.com/OmniSharp/csharp-language-server-protocol">C#-LSP</see>
/// and <see
/// href="https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/">LSP
/// Spec</see>.
/// </summary>
public class TextDocumentSyncHandler : TextDocumentSyncHandlerBase
{
    private readonly BufferManager bufManager;
    private readonly ILanguageServerFacade languageServer;
    private readonly TSParser parser;
    private readonly Analyzer.Analyzer analyzer;

    public TextDocumentSyncHandler(BufferManager manager, ILanguageServerFacade languageServer)
    {
        bufManager = manager;
        parser = new TSParser(TSJsonLanguage.Language());
        this.languageServer = languageServer;
        analyzer = new Analyzer.Analyzer(new TSQuery(@"(ERROR) @error", TSJsonLanguage.Language()));
    }

    public override TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
    {
        // Language ID of json and jsonc are just their names
        // which are also the extensions of the files.
        return new TextDocumentAttributes(uri, Path.GetExtension(uri.Path));
    }

    public override Task<Unit> Handle(
        DidOpenTextDocumentParams request,
        CancellationToken cancellationToken
    )
    {
        bufManager.Add(request.TextDocument.Uri, CreateBuffer(request.TextDocument.Text));

        AnalyzeWorkspace();

        return Unit.Task;
    }

    public override Task<Unit> Handle(
        DidChangeTextDocumentParams request,
        CancellationToken cancellationToken
    )
    {
        var text = request.ContentChanges.FirstOrDefault()?.Text;
        if (text is not null)
        {
            bufManager.Add(request.TextDocument.Uri, CreateBuffer(text));
            AnalyzeWorkspace();
        }

        return Unit.Task;
    }

    public override Task<Unit> Handle(
        DidSaveTextDocumentParams request,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    public override Task<Unit> Handle(
        DidCloseTextDocumentParams request,
        CancellationToken cancellationToken
    )
    {
        throw new NotImplementedException();
    }

    public override string? ToString()
    {
        return base.ToString();
    }

    protected override TextDocumentSyncRegistrationOptions CreateRegistrationOptions(
        TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities
    )
    {
        return new TextDocumentSyncRegistrationOptions(TextDocumentSyncKind.Full);
    }

    /// <summary>
    /// Utility method to create a <see cref="Buffer" /> out of provided `text`.
    /// </summary>
    private Buffer.Buffer CreateBuffer(string text)
    {
        return new Buffer.Buffer(text, parser.ParseString(text));
    }

    /// <summary>
    /// This method is responsible for analyzing and publishing
    /// diagnostics about all buffers to language client.
    /// </summary>
    private void AnalyzeWorkspace()
    {
        var diagnostics = analyzer.Analyze(bufManager.GetBuffers());

        foreach (var buf in diagnostics)
        {
            languageServer.SendNotification(
                new PublishDiagnosticsParams() { Uri = buf.Key, Diagnostics = buf.Value.ToList() }
            );
        }
    }
}
