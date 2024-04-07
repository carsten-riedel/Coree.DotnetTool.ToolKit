﻿using System.Threading.Tasks;

using Coree.DotnetTool.ToolKit.Command;
using Coree.NETStandard.CoreeHttpClient;
using Coree.NETStandard.Serilog;
using Coree.NETStandard.Services;
using Coree.NETStandard.SpectreConsole;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureServices(service =>
            {
                service.AddCoreeHttpClient();
                service.AddLoggingCoreeNETStandard(true,
                    new System.Collections.Generic.Dictionary<string, Serilog.Events.LogEventLevel>() {
                        { "System.Net.Http.HttpClient.CoreeHttpClient.LogicalHandler", Serilog.Events.LogEventLevel.Debug },
                        { "System.Net.Http.HttpClient.CoreeHttpClient.ClientHandler", Serilog.Events.LogEventLevel.Debug }
                    });
                service.AddSingleton<IFileService, FileService>();
                service.AddSingleton<IProcessService, ProcessService>();

                service.AddSpectreConsole(configureCommandApp =>
                {
                    configureCommandApp.SetApplicationName("toolkit");
                    configureCommandApp.AddCommand<CommandExistsAsyncCommand>("command-exists").WithExample(new[] { "command-exists", "curl" }).WithExample(new[] { "command-exists", "foo", "-t", "-l Fatal" });
                    configureCommandApp.AddCommand<SetenvGitrootAsyncCommand>("setenv-gitroot");
                    configureCommandApp.AddCommand<SetenvGitbranchAsyncCommand>("setenv-gitbranch");
                    configureCommandApp.AddCommand<NugetDelistAsyncCommand>("nuget-delist");
                    configureCommandApp.AddCommand<SelfUpdateAsyncCommand>("selfupdate").WithExample(new[] { "selfupdate" });
                    //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    //{
                    //}
                });
            });

            await builder.Build().RunAsync();
        }
    }
}