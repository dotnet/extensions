// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.WindowsService
{
    public class ServiceBaseLifetime : ServiceBase, IHostLifetime
    {
        private TaskCompletionSource<object> _delayStart = new TaskCompletionSource<object>();

        public ServiceBaseLifetime(IHostingEnvironment environment, IApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory)
        {
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
            Logger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
        }

        public IHostingEnvironment Environment { get; }
        private IApplicationLifetime ApplicationLifetime { get; }
        private ILogger Logger { get; }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _delayStart.TrySetCanceled());
            ApplicationLifetime.ApplicationStarted.Register(() =>
            {
                Logger.LogInformation("Application started. Hosting environment: {envName}; Content root path: {contentRoot}",
                    Environment.EnvironmentName, Environment.ContentRootPath);
            });
            ApplicationLifetime.ApplicationStopping.Register(() =>
            {
                Logger.LogInformation("Application is shutting down...");
            });

            new Thread(Run).Start(); // Otherwise this would block and prevent IHost.StartAsync from finishing.
            return _delayStart.Task;
        }

        private void Run()
        {
            try
            {
                Run(this); // This blocks until the service is stopped.
                _delayStart.TrySetException(new InvalidOperationException("Stopped without starting"));
            }
            catch (Exception ex)
            {
                _delayStart.TrySetException(ex);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stop();
            return Task.CompletedTask;
        }

        // Called by base.Run when the service is ready to start.
        protected override void OnStart(string[] args)
        {
            _delayStart.TrySetResult(null);
            base.OnStart(args);
        }

        // Called by base.Stop. This may be called multiple times by service Stop, ApplicationStopping, and StopAsync.
        // That's OK because StopApplication uses a CancellationTokenSource and prevents any recursion.
        protected override void OnStop()
        {
            ApplicationLifetime.StopApplication();
            base.OnStop();
        }
    }
}
