using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Armls.Buffer;
using Armls.Handlers;
using Armls.Schema;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Schema;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Armls;

public class Program
{
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

        var server = await LanguageServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithServices(s =>
                    s.AddSingleton(new BufferManager()).AddSingleton(new MinimalSchemaComposer())
                )
                .WithHandler<TextDocumentSyncHandler>()
                .WithHandler<HoverHandler>()
                .WithHandler<CompletionHandler>()
        );

        await server.WaitForExit;
    }
}
