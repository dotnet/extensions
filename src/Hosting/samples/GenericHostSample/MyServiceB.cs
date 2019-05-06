using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GenericHostSample
{
    public class MyServiceB : IHostedService, IDisposable
    {
        private bool _stopping;
        private Task _backgroundTask;

        public MyServiceB(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<MyServiceB>();
        }

        public ILogger Logger { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("MyServiceB is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(7));
                Logger.LogInformation("MyServiceB is doing background work.");
            }

            Logger.LogInformation("MyServiceB background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("MyServiceB is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }

        public void Dispose()
        {
            Logger.LogInformation("MyServiceB is disposing.");
        }
    }
}
