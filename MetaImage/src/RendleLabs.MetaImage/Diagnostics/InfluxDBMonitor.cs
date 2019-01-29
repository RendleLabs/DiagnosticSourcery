using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RendleLabs.InfluxDB;
using RendleLabs.InfluxDB.DiagnosticSourceListener;
using RendleLabs.MetaImage.Options;

namespace RendleLabs.MetaImage.Diagnostics
{
    public class InfluxDBMonitor : IHostedService
    {
        private readonly ILogger<InfluxDBMonitor> _logger;
        private readonly IInfluxDBClient _client;
        private IDisposable _subscription;

        public InfluxDBMonitor(IOptions<InfluxDBOptions> options, ILogger<InfluxDBMonitor> logger)
        {
            _logger = logger;
            if (!string.IsNullOrWhiteSpace(options.Value.Server) && !string.IsNullOrWhiteSpace(options.Value.Database))
            {
                _client = new InfluxDBClientBuilder(options.Value.Server, options.Value.Database)
                    .ForceFlushInterval(TimeSpan.FromSeconds(10))
                    .OnError(LogError)
                    .Build();
            }
        }

        private void LogError(Exception ex)
        {
            _logger.LogError(ex, "Error in InfluxDB DiagnosticSource listener");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_client != null)
            {
                _subscription = DiagnosticSourceInfluxDB.Listen(_client, _ => true);
            }
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _subscription?.Dispose();
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
            return Task.CompletedTask;
        }
    }
}