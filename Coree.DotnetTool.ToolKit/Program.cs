using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading.Tasks;
using System.Threading;
using Serilog;
using System.Diagnostics;

namespace Coree.DotnetTool.ToolKit
{
    public class Program
    {
        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public static int ExitCode = 0;

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
                    .Enrich.With(new SourceContextShortEnricher())
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.ffff} | {Level:u3} | {SourceContextShort} | {Message:l}{NewLine}{Exception}")
                    .CreateLogger()
                )
            );

            services.AddSingleton<ILoggingService, ConsoleLoggingService>();

            var registrar = new TypeRegistrar(services);

            // Create a new command app with the registrar
            // and run it with the provided arguments.
            var app = new CommandApp(registrar);
            app.Configure(config =>
            {
                config.SetApplicationName("toolkit");
                config.AddCommand<HelloAsyncCommand>("hello");
                config.AddCommand<HelloAsyncCommand>("hellox");
            });

            return await app.RunAsync(args);
        }
    }
}