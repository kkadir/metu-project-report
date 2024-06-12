using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CommandLine;
using Serilog;
using Serilog.Extensions.Logging;
using Metu.MarketData.Client.Options;
using Metu.MarketData.Client.Services;

namespace Metu.MarketData.Client;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        try
        {
            Log.Information("Starting METU Market Data Client..");
            await Host.CreateDefaultBuilder(args)
                    .ConfigureLogging((ctx, config) =>
                    {
                        AddLogging(config, ctx.Configuration);
                    })
                    .ConfigureServices((ctx, services) =>
                    {
                        AddServiceConfigurations(ctx, services, args, cancellationTokenSource);
                        AddHostedServices(services);
                    }).RunConsoleAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Log.Fatal("Application cancelled, see previous errors");
        }
        catch (Exception e)
        {
            Log.Fatal(e, "The application failed to start correctly");
        }
        finally
        {
            Log.Information("Shutting down application");
            Log.CloseAndFlush();
        }
    }

    private static void AddLogging(ILoggingBuilder config, IConfiguration Configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        config.ClearProviders();
        config.AddProvider(new SerilogLoggerProvider(Log.Logger));

        var minimumLevel = Configuration.GetSection("Serilog:MinimumLevel")?.Value;
        if (!string.IsNullOrEmpty(minimumLevel))
        {
            config.SetMinimumLevel(Enum.Parse<LogLevel>(minimumLevel));
        }
    }

    private static void AddServiceConfigurations(HostBuilderContext ctx,
                                                 IServiceCollection services,
                                                 string[] args,
                                                 CancellationTokenSource cancellationTokenSource)
    {
        services.Configure<ConnectionSettings>(ConnectionSettings.Kdb, ctx.Configuration.GetSection("ConnectionSettings:Kdb"));

        Parser.Default.ParseArguments<CommandLineOptions>(args)
                      .MapResult(o => RunOptionsAndReturnExitCode(o,
                                                                  services,
                                                                  cancellationTokenSource), e => HandleParseError(e,
                                                                                                                  cancellationTokenSource));

        LimitsRegulator(services);
    }

    private static int RunOptionsAndReturnExitCode(CommandLineOptions options,
                                                   IServiceCollection services,
                                                   CancellationTokenSource cancellationTokenSource)
    {
        services.PostConfigureAll<ConnectionSettings>(o =>
        {
            if (!string.IsNullOrWhiteSpace(options.Host)) o.Host = options.Host;
            if (options.Port > 0) o.Port = options.Port;
            if (options.ConnectionCount > 0) o.ConnectionCount = options.ConnectionCount;
            if (!string.IsNullOrWhiteSpace(options.SymbolFile)) o.SymbolFile = options.SymbolFile;
            if (options.SubscriptionsCount > 0) o.SubscriptionsCount = options.SubscriptionsCount;
            if (options.ListenDuration > 0) o.ListenDuration = options.ListenDuration;
            if (options.InstanceNum > 0) o.InstanceNum = options.InstanceNum;
        });

        return 0;
    }

    private static int HandleParseError(IEnumerable<Error> errors, CancellationTokenSource cancellationTokenSource)
    {
        var exitCode = -2;

        if (errors.Any(x => x is HelpRequestedError || x is VersionRequestedError))
        {
            exitCode = -1;
        }

        Log.Fatal($"Exit Code: {exitCode}");
        cancellationTokenSource.Cancel();

        return exitCode;
    }

    private static void LimitsRegulator(IServiceCollection services)
    {
        services.PostConfigureAll<ConnectionSettings>(o =>
        {
            var maxConnections = 100;
            var maxSubscriptions = 12000;

            if (o.ConnectionCount > maxConnections)
            {
                Log.Warning($"Connection count shouldn't exceed {maxConnections}. Defaulting to maximum.");
                o.ConnectionCount = maxConnections;
            }

            if (o.SubscriptionsCount > maxSubscriptions)
            {
                Log.Warning($"Subscriptions count shouldn't exceed {maxSubscriptions}. Defaulting to maximum.");
                o.SubscriptionsCount = maxSubscriptions;
            }
        });
    }

    private static void AddHostedServices(IServiceCollection services)
    {
        services.AddHostedService<MarketDataService>();
        services.AddTransient<IMdService, KdbMdService>();
    }
}
