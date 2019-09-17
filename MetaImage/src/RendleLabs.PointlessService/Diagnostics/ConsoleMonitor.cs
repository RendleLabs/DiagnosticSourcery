using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RendleLabs.PointlessService.Diagnostics
{
    public class ConsoleMonitor : IHostedService
    {
        private readonly ILogger<ConsoleMonitor> _logger;
        private IDisposable _allListenersSubscription;
        private readonly ConcurrentBag<IDisposable> _subscriptions = new ConcurrentBag<IDisposable>();

        public ConsoleMonitor(ILogger<ConsoleMonitor> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _allListenersSubscription = DiagnosticListener.AllListeners.Do(source =>
            {
                _subscriptions.Add(source.Do(pair =>
                {
                    Log(source.Name, pair.Key, pair.Value);
                })
                .Subscribe());
            }).Subscribe();

            return Task.CompletedTask;
        }

        private void Log(string source, string key, object args)
        {
            if (args == null)
            {
                _logger.LogInformation("Source: {source}, Key: {key}", source, key);
                return;
            }
            
            _logger.LogInformation("Source: {source}, Key: {key}, Args: {args}", source, key, Serialize(args));
        }

        private string Serialize(object arg)
        {
            StringBuilder builder = null;
            foreach (var property in arg.GetType().GetProperties())
            {
                var value = property.GetValue(arg);
                if (value == null) continue;
                if (builder == null)
                {
                    builder = new StringBuilder($"{{ {property.Name} = '{value}'");
                }
                else
                {
                    builder.Append($", {property.Name} = '{value}'");
                }
            }

            return builder?.Append(" }").ToString();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _allListenersSubscription?.Dispose();
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}