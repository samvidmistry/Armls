using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Armls.Buffer;
using Armls.Handlers;
using Armls.Schema;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Armls;

public class Program
{
    private static readonly Regex ManifestResoureceRegex1 = new(
        @".+?\.schemas\._([0-9]+_[0-9]+_[0-9]+(.*?)?)\.(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex ManifestResoureceRegex2 = new(
        @".+?\.schemas\._([0-9]+\._[0-9]+\._[0-9]+)(.*?)?\.(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex ManifestResoureceRegex3 = new(
        @".+?\.(schemas\.common\.)(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    private static readonly Regex ManifestResoureceRegex4 = new(
        @".+?\.schemas\.viewdefinition\._([0-9]+\._[0-9]+\._[0-9]+)(.*?)?\.(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public static void Main()
    {
        MainAsync().Wait();
    }

    private static async Task MainAsync()
    {
        // Debugger.Launch();
        // while (!Debugger.IsAttached)
        // {
        //     await Task.Delay(100);
        // }

        // Load and map all schema resources to their paths to be used
        // by <see cref="SchemaHandler" />.
        var assembly = Assembly.GetExecutingAssembly();
        var resources = assembly
            .GetManifestResourceNames()
            .Where(r => r.EndsWith(".json"))
            .Select(r =>
                KeyValuePair.Create(
                    ConvertManifestResourceNameToPath(r),
                    assembly.GetManifestResourceStream(r)
                )
            )
            .ToDictionary();

        var server = await LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithServices(s =>
                    s.AddSingleton(new BufferManager()).AddSingleton(new SchemaHandler(resources))
                )
                .WithHandler<TextDocumentSyncHandler>()
        );

        await server.WaitForExit;
    }

    private static string ConvertManifestResourceNameToPath(string resourceName)
    {
        var match = ManifestResoureceRegex1.Match(resourceName);
        if (match.Success)
        {
            var apiVersion = match.Groups[1].Value.Replace('_', '-');
            var fileName = match.Groups[3].Value;

            return $"schemas/{apiVersion}/{fileName}";
        }

        match = ManifestResoureceRegex2.Match(resourceName);
        if (match.Success)
        {
            var apiVersion = match.Groups[1].Value.Replace("_", "");
            var preview = match.Groups[2].Value.Replace('_', '-');
            var fileName = match.Groups[3].Value;

            return $"schemas/{apiVersion}{preview}/{fileName}";
        }

        match = ManifestResoureceRegex3.Match(resourceName);
        if (match.Success)
        {
            var schemaName = match.Groups[1].Value.Replace('.', '/');
            var fileName = match.Groups[2].Value;

            return $"{schemaName}{fileName}";
        }

        match = ManifestResoureceRegex4.Match(resourceName);
        if (match.Success)
        {
            var apiVersion = match.Groups[1].Value.Replace("_", "");
            var preview = match.Groups[2].Value.Replace("_", "-");
            var fileName = match.Groups[3].Value;

            return $"schemas/viewdefinition/{apiVersion}{preview}/{fileName}";
        }

        throw new InvalidOperationException("All resource names much match one of the Regexes");
    }
}
