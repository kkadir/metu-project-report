using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Metu.MarketData.Client.Options;
using Metu.MarketData.Client.Extensions;
using Metu.MarketData.Kdb;
using Microsoft.Extensions.Hosting;
using System.Globalization;


namespace Metu.MarketData.Client.Services;

public class KdbMdService : IMdService
{
    private readonly ILogger _logger;
    private readonly Serilog.ILogger _metricLogger;
    private ConnectionSettings _settings;
    private readonly IHostApplicationLifetime _lifetime;
    private ConcurrentDictionary<int, (bool? Connected, QCallbackConnection Connection)> _connections;
    private bool LogMetricFlag;


    public KdbMdService(ILogger<KdbMdService> logger, IOptionsSnapshot<ConnectionSettings> settings, IHostApplicationLifetime lifetime)
    {
        _metricLogger = Serilog.Log.ForContext("metric", "set");
        _metricLogger.Information(
            $"Connection#,Topic,Count,Size (Bytes),Publish Time (UTC),Log Time (UTC),Travel Elapsed (ms),E2E Elapsed (ms),MDS Elapsed (ms)"
        );
        _logger = logger;
        _settings = settings.Get(ConnectionSettings.Kdb);
        _lifetime = lifetime;
        _connections = new ConcurrentDictionary<int, (bool?, QCallbackConnection)>(_settings.ConnectionCount, _settings.ConnectionCount);
    }

    public async Task SubscribeAndListenAsync(IEnumerable<string> symbols, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Establishing #{_settings.ConnectionCount} {ConnectionSettings.Kdb} connections");
        await EstablishConnections(_settings.ConnectionCount, cancellationToken);
        var failedConnections = _connections.Where(c => !c.Value.Connected.HasValue || (c.Value.Connected.HasValue && !c.Value.Connected.Value));
        var failedConnectionsCount = failedConnections.Count();

        if (failedConnectionsCount > 0)
        {
            _logger.LogError($"{failedConnectionsCount}/{_settings.ConnectionCount} failed to establish a connection!");
            if (failedConnectionsCount == _settings.ConnectionCount)
            {
                _logger.LogCritical("All connections failed, exiting application.");
                _lifetime.StopApplication();
            }
        }
        else
        {
            _logger.LogInformation("All connections successfully established.");
        }

        _logger.LogInformation($"Subscribing to #{symbols.Count()} symbols and start listening..");
        await Subscribe(symbols, cancellationToken);
        _logger.LogInformation("Successfully subscribed, now listening");

        LogMetricFlag = true;
        await Task.Delay(TimeSpan.FromSeconds(_settings.ListenDuration));


        _logger.LogInformation($"Unsubscribing from #{symbols.Count()} symbols..");
        await Unsubscribe();
        _logger.LogInformation("Successfully unsubscribed.");
    }

    private async Task EstablishConnections(int connectionCount, CancellationToken cancellationToken)
    {
        var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.LoginDuration));
        var timeoutToken = timeoutSource.Token;

        var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);
        var combinedToken = combinedSource.Token;

        var source = Enumerable.Range(0, connectionCount);
        await Parallel.ForEachAsync<int>(source, cancellationToken, async (i, ct) =>
        {
            await Task.Run(async () =>
            {
                var connection = new QCallbackConnection(_settings.Host, _settings.Port, connectionId: i);
                connection.DataReceived += async (sender, eventArgs) => await OnData(sender, eventArgs);
                connection.ErrorOccured += async (sender, error) => await OnError(sender, error);

                _connections.TryAdd(i, (true, connection));

                try
                {
                    connection.Open();
                    connection.StartListener(ct);

                    _logger.LogInformation($"Connection #{i} created.");
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, $"Connection #{i} failed to to be created with exception.");
                    return;
                }

                try
                {
                    while (!_connections[connection.ConnectionId].Connected.HasValue)
                    {
                        if (timeoutToken.IsCancellationRequested)
                        {
                            _logger.LogError($"Connection #{i} timed out.");
                            break;
                        }
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, $"Connection #{i} failed to login with exception.");
                }

            }, combinedToken);
        });
    }

    private async Task Subscribe(IEnumerable<string> symbols, CancellationToken cancellationToken)
    {
        var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(_settings.LoginDuration));
        var timeoutToken = timeoutSource.Token;

        var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutToken);
        var combinedToken = combinedSource.Token;

        var succeededConnections = _connections.Where(c => c.Value.Connected.HasValue && c.Value.Connected.Value).ToArray();
        var source = Enumerable.Range(0, succeededConnections.Count());

        var totalConnections = succeededConnections.Count();
        var batchSize = symbols.Count() / totalConnections;

        await Parallel.ForEachAsync<int>(source, cancellationToken, async (i, ct) =>
        {
            await Task.Run(() =>
            {
                var connection = succeededConnections[i].Value.Connection;
                var batch = symbols.Skip(i * batchSize).Take(batchSize);

                var subscribeTrade = new QDictionary(
                                    new[] { "topic", "syms", "ref", "includeLatencyStats"},
                                    new object[] { "trade", batch.ToArray<string>(), $"trade-test-{connection.ConnectionId}".ToCharArray(), true });
                var subscribeNbbO = new QDictionary(
                                   new[] { "topic", "syms", "ref", "includeLatencyStats"},
                                    new object[] { "nbbo", batch.ToArray<string>(), $"nbbo-test-{connection.ConnectionId}".ToCharArray(), true });
             
                connection.Async("subscribe", subscribeTrade);
                connection.Async("subscribe", subscribeNbbO);
            }, combinedToken);
        });
    }

    private async Task Unsubscribe()
    {
        await Task.Delay(System.TimeSpan.FromSeconds(1));
    }

    private async Task OnData(object sender, QMessageEvent e)
    {
        await Task.Run(() =>
        {
            var connectionId = 0;
            if (sender is QCallbackConnection connection) connectionId = connection.ConnectionId;
            else { return; }

            if (e.Message.Data is QDictionary data && data.Keys.Length > 0)
            {
                var values = data.Values as object[];
                var topic = data.Keys.GetValue(0) as string;
                var topicData = values[0] as string;



                if (topic == "login")
                {
                    LogMetrics(e.Message, connectionId, "login");

                    var loginResult = values[1];
                    var loginConnectionId = connectionId;
                    var loginStatus = true;

                    if (loginResult is not bool)
                    {
                        if (loginResult is char[] loginResultMessage)
                        {
                            _logger.LogWarning($"Connection #{connectionId} failed with a message: {new string(loginResultMessage, 0, loginResultMessage.Length)}");
                        }
                        else
                        {
                            _logger.LogWarning($"Connection #{connectionId} failed for some reason");
                        }

                        loginStatus = false;
                    }

                    _connections.AddOrUpdate(
                        loginConnectionId,
                        (loginStatus, connection),
                        (key, oldTuple) => (loginStatus, connection));
                }
                else if (topic != "topic")
                {
                    LogMetrics(e.Message, connectionId, topic);
                }
                else
                {
                    LogMetrics(e.Message, connectionId, topicData);
                }
            }
        });
    }

    private async Task OnError(object sender, QErrorEvent error)
    {
        await Task.Run(() =>
        {
            var connectionId = 0;
            if (sender is QCallbackConnection connection) connectionId = connection.ConnectionId;
            else { return; }

            var errorMessage = $"(#{connectionId}) Error received via callback: {error.Cause.Message}";
            _logger.LogError(errorMessage);
        });
    }

    
    private void LogMetrics(QMessage message, int connectionId, string topic)
    {
        if (!LogMetricFlag) return;

        Task.Run(() =>
        {
            var rowCount = 0;
            if (message.Data is QDictionary data && data.Keys.Length > 0)
            {
                if (data.Values is object[] { Length: > 1 } values)
                {
                    var table = values[1] as QTable;
                    rowCount = table?.RowsCount ?? 0;
                }
            }

            var latency = message.QGetTime();

            var publishTime = latency.publishTime;
            var recvTime = latency.receiveTime;
            var logTime = DateTime.UtcNow;
            var travelDiff = TimeSpan.FromTicks(logTime.Ticks - publishTime.Ticks).TotalMilliseconds;
            var e2eDiff = TimeSpan.FromTicks(logTime.Ticks - recvTime.Ticks).TotalMilliseconds;
            var pubDiff = TimeSpan.FromTicks(publishTime.Ticks - recvTime.Ticks).TotalMilliseconds;

            _metricLogger.Information(
                $"{_settings.InstanceNum}_{connectionId},{topic},{rowCount},{message.DataSize},{publishTime.ToString("HH:mm:ss.fffffff")},{logTime.ToString("HH:mm:ss.fffffff")},{travelDiff.ToString(CultureInfo.InvariantCulture)},{e2eDiff.ToString(CultureInfo.InvariantCulture)},{pubDiff.ToString(CultureInfo.InvariantCulture)}"
            );
        });
    }
}
