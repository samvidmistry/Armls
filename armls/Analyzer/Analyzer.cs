using Armls.Schema;
using Armls.TreeSitter;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
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
    private readonly MinimalSchemaComposer schemaComposer;

    /// <summary>
    /// Creates an instance of Analyzer for ARM template validation.
    /// Uses dynamic schema composition to load only the schemas needed
    /// for the resource types actually used in templates.
    /// </summary>
    public Analyzer(TSQuery errorQuery, MinimalSchemaComposer schemaComposer)
    {
        this.errorQuery = errorQuery;
        this.schemaComposer = schemaComposer;
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

            // Extract resource types with their API versions from the ARM template
            var resourceTypesWithVersions = buf.GetResourceTypes();

            // Compose minimal schema with only needed resource definitions
            var schema = await schemaComposer.ComposeSchemaAsync(
                schemaUrl,
                resourceTypesWithVersions
            );

            if (schema is null)
            {
                diagnostics[path] = new List<Diagnostic>
                {
                    new Diagnostic
                    {
                        Range = new OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(
                            new Position(0, 0),
                            new Position(0, 0)
                        ),
                        Message = $"Failed to load schema {schemaUrl}",
                        Severity = DiagnosticSeverity.Warning,
                    },
                };
                continue;
            }

            IList<ValidationError> errors;
            var isValid = JToken.Parse(buf.Text).IsValid(schema, out errors);
            diagnostics[path] = GetLeafErrors(errors)
                .Where(e => !e.Message.Contains("Expected Object but got Array."))
                .Select(e => new Diagnostic
                {
                    Range = buf
                        .ConcreteTree.RootNode()
                        .NamedDescendantForPointRange(
                            new TSPoint
                            {
                                row = (uint)e.LineNumber - 1,
                                column = (uint)e.LinePosition - 1,
                            },
                            new TSPoint
                            {
                                row = (uint)e.LineNumber - 1,
                                column = (uint)e.LinePosition - 1,
                            }
                        )
                        .GetRange(),
                    Message = e.Message,
                    Severity = DiagnosticSeverity.Warning,
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

    private List<ValidationError> GetLeafErrors(IList<ValidationError> errors)
    {
        var leafErrors = new List<ValidationError>();
        foreach (var error in errors)
        {
            if (error.ChildErrors == null || error.ChildErrors.Count == 0)
            {
                leafErrors.Add(error);
            }
            else
            {
                leafErrors.AddRange(GetLeafErrors(error.ChildErrors));
            }
        }
        return leafErrors;
    }
}
