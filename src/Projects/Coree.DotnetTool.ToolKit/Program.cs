using System;
using System.Threading;
using System.Threading.Tasks;
using Coree.DotnetTool.ToolKit.Command;
using Coree.NETStandard.Extensions;
using Coree.NETStandard.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Spectre.Console.Cli;

namespace Coree.DotnetTool.ToolKit
{
    public class Program
    {
        internal static LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch();
        internal static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        internal static int ExitCode = -1;

        public static async Task<int> Main(string[] args)
        {
            Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) =>
                {
                    var isCtrlC = e.SpecialKey == ConsoleSpecialKey.ControlC;
                    var isCtrlBreak = e.SpecialKey == ConsoleSpecialKey.ControlBreak;

                    if (isCtrlC || isCtrlBreak)
                    {
                        cancellationTokenSource.Cancel();
                        Environment.Exit(ExitCode);
                    }
                };

            ServiceCollection services = new ServiceCollection();

            services.AddLogging(configure =>
                configure.AddSerilog(new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .Enrich.With(new SourceContextShortEnricher())
                    .MinimumLevel.ControlledBy(levelSwitch)
                    .WriteTo.Console(outputTemplate: SerilogExtensions.SimpleOutputTemplate())
                    .CreateLogger()
                )
            );


            services.AddSingleton<Coree.NETStandard.Services.IFileService, Coree.NETStandard.Services.FileService>();
            services.AddSingleton<LoggingLevelSwitch>(levelSwitch);

            var registrar = new Coree.NETStandard.TypeRegistrar(services);

            // Create a new command app with the registrar
            // and run it with the provided arguments.
            var app = new CommandApp(registrar);
            app.Configure(config =>
            {
                config.SetApplicationName("toolkit");
                config.AddCommand<CommandExistsAsyncCommand>("command-exists");
            });

            return await app.RunAsync(args);
        }
    }
}