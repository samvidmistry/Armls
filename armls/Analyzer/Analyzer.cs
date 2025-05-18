using Armls.TreeSitter;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Analyzer;

public class Analyzer
{
    private readonly TSQuery errorQuery;

    public Analyzer(TSQuery errorQuery)
    {
        this.errorQuery = errorQuery;
    }

    public IDictionary<string, IEnumerable<Diagnostic>> Analyze(
        IReadOnlyDictionary<string, Buffer.Buffer> buffers
    )
    {
        return buffers
            .Select(kvp => new KeyValuePair<string, IEnumerable<Diagnostic>>(
                kvp.Key,
                AnalyzeBuffer(kvp.Value)
            ))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private IEnumerable<Diagnostic> AnalyzeBuffer(Buffer.Buffer buf)
    {
        IEnumerable<Diagnostic> diagnostics = new List<Diagnostic>();
        var cursor = errorQuery.Execute(buf.ConcreteTree.RootNode());
        while (cursor.Next(out TSQueryMatch? match))
        {
            diagnostics = match!
                .Captures()
                .Select(n => new Diagnostic()
                {
                    Range = n.GetRange(),
                    Severity = DiagnosticSeverity.Error,
                    Source = "armls",
                    Message = "Syntax error", // TODO: Update this to list the expected symbols
                })
                .Concat(diagnostics);
        }

        return diagnostics;
    }
}
