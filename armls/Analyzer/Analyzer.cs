using Armls.Schema;
using Armls.TreeSitter;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Armls.Analyzer;

/// <summary>
/// Analyzer provides errors, warnings, and diagnostics about an ARM
/// template. Look at the documentation of <see
/// cref="AnalyzeAsync(IReadOnlyDictionary{string, Armls.Buffer.Buffer})"
/// /> for a list of analyses provided.
/// </summary>
public class Analyzer
{
    private readonly TSQuery errorQuery;
    private readonly SchemaHandler schemaHandler;

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
    public Analyzer(TSQuery errorQuery, SchemaHandler schemaHandler)
    {
        this.errorQuery = errorQuery;
        this.schemaHandler = schemaHandler;
    }

    /// <summary>
    /// Analyze the given dictionary of <see cref="Buffer" /> for
    /// various issues. The analyses currently provided are:
    /// 1. Syntax checking
    /// 2. Schema checking
    /// </summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlyList<Diagnostic>>> AnalyzeAsync(
        IReadOnlyDictionary<string, Buffer.Buffer> buffers
    )
    {
        var diagnostics = new Dictionary<string, IReadOnlyList<Diagnostic>>();

        foreach (var (path, buf) in buffers)
        {
            var syntaxErrors = AnalyzeBuffer(buf);
            if (syntaxErrors.Count() != 0)
            {
                diagnostics[path] = syntaxErrors;
                continue;
            }

            // Only schema check the syntactically valid files
            var schemaUrl = buf.GetStringValue(@"$schema");
            if (schemaUrl is null)
            {
                continue;
            }

            var errors = await schemaHandler.ValidateAsync(schemaUrl, buf.Text);
            diagnostics[path] = errors
                .Select(e => new Diagnostic
                {
                    Range = buf
                        .ConcreteTree.RootNode()
                        .DescendantForPoint(
                            new TSPoint
                            {
                                row = (uint)e.LineNumber - 1,
                                column = (uint)e.LinePosition - 1,
                            }
                        )
                        .GetRange(),
                    Message = e.Message,
                })
                .ToList();
        }

        return diagnostics;
    }

    /// <summary>
    /// Analyzes a single <see cref="Buffer" /> for issues. It returns
    /// a collection of <see cref="Diagnostic" /> describing issues
    /// with the buffer.
    /// </summary>
    private IReadOnlyList<Diagnostic> AnalyzeBuffer(Buffer.Buffer buf)
    {
        var diagnostics = new List<Diagnostic>();
        var cursor = errorQuery.Execute(buf.ConcreteTree.RootNode());
        while (cursor.Next(out TSQueryMatch? match))
        {
            var diags = match!
                .Captures()
                .Select(n => new Diagnostic()
                {
                    Range = n.GetRange(),
                    Severity = DiagnosticSeverity.Error,
                    Source = "armls",
                    Message = "Syntax error", // TODO: Update this to list the expected symbols
                });

            diagnostics.AddRange(diags);
        }

        return diagnostics;
    }
}
