using Armls.TreeSitter;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Analyzer;

/// <summary>
/// Analyzer provides errors, warnings, and diagnostics about an ARM
/// template. Look at the documentation of <see
/// cref="Analyze(IReadOnlyDictionary{string, Armls.Buffer.Buffer})"
/// /> for a list of analyses provided.
/// </summary>
public class Analyzer
{
    private readonly TSQuery errorQuery;

    /// <summary>
    /// Creates an instance of Analyzer. To keep it independent of
    /// which language it is syntax checking, I take in a <see
    /// cref="TSQuery" /> which queries error nodes out of parse tree.
    /// I was hoping to keep the analyzer independent of the
    /// underlying language it is analyzing. But I am now realizing
    /// that it won't really be possible to separate the analysis from
    /// the underlying mechanism of describing the configuration,
    /// which is through JSON. Let's see how it goes.
    /// </summary>
    public Analyzer(TSQuery errorQuery)
    {
        this.errorQuery = errorQuery;
    }

    /// <summary>
    /// Analyze the given dictionary of <see cref="Buffer" /> for
    /// various issues. The analyses currently provided are:
    /// 1. Syntax checking
    /// </summary>
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

    /// <summary>
    /// Analyzes a single <see cref="Buffer" /> for issues. It returns
    /// a collection of <see cref="Diagnostic" /> describing issues
    /// with the buffer.
    /// </summary>
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
