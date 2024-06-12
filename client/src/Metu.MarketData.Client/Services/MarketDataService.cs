using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Metu.MarketData.Client.Extensions;
using Metu.MarketData.Client.Options;

namespace Metu.MarketData.Client.Services;

internal sealed class MarketDataService : IHostedService
{
    private int? _exitCode;
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ConnectionSettings _settings;
    private List<string> _symbols = [];
    private List<string> _imbSymbols = [];
    private readonly IMdService _mdService;

    public MarketDataService(ILogger<MarketDataService> logger, IHostApplicationLifetime appLifetime, IOptionsSnapshot<ConnectionSettings> settings, IMdService mdService)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _settings = settings.Get(ConnectionSettings.Kdb);
        _mdService = mdService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Starting with arguments: {string.Join(" ", Environment.GetCommandLineArgs())}");

        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation($"Host: {_settings.Host}");
                    _logger.LogInformation($"Port: {_settings.Port}");
                    _logger.LogInformation($"Connection#: {_settings.ConnectionCount}");
                    _logger.LogInformation($"Symbol File#: {_settings.SymbolFile}");
                    _logger.LogInformation($"Subscriptions Count#: {_settings.SubscriptionsCount}");


                    // Load Symbol File
                    try
                    {
                        _symbols = (await File.ReadAllLinesAsync(_settings.SymbolFile, cancellationToken)).ToList();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Cannot read file with exception");
                        _appLifetime.StopApplication();
                    }

                    _logger.LogInformation($"Symbols: {_symbols.Count}");

                    var randomSymbols = _symbols.GetRandom(_settings.SubscriptionsCount);
                    var definiteSymbols = _symbols.GetRange(0, _settings.SubscriptionsCount);

                    _logger.LogInformation($"Symbols => {string.Join(",", definiteSymbols)}");


                    _logger.LogInformation($"Subscribing and listening:");
                    await _mdService.SubscribeAndListenAsync(definiteSymbols, cancellationToken);

                    _exitCode = 0;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception!");
                    _exitCode = 1;
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            });
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Exiting with return code: {_exitCode}");

        Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
        return Task.CompletedTask;
    }
}