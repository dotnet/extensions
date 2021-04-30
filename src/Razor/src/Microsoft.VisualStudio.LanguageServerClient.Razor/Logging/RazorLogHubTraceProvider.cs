// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.Logging;
using VSShell = Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Logging
{
    [Shared]
    [Export(typeof(RazorLogHubTraceProvider))]
    internal class RazorLogHubTraceProvider
    {
        private static readonly LoggerOptions _logOptions = new(
            requestedLoggingLevel: new LoggingLevelSettings(SourceLevels.Information | SourceLevels.ActivityTracing),
            privacySetting: PrivacyFlags.MayContainPersonallyIdentifibleInformation | PrivacyFlags.MayContainPrivateInformation);

        private readonly SemaphoreSlim _initializationSemaphore = null;
        private IServiceBroker _serviceBroker = null;

        public RazorLogHubTraceProvider()
        {
            _initializationSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        }

        public async Task<TraceSource> InitializeTraceAsync(string logIdentifier, int logHubSessionId, CancellationToken cancellationToken)
        {
            if (!await TryInitializeServiceBrokerAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            var _logId = new LogId(
                logName: $"{logIdentifier}.{logHubSessionId}",
                serviceId: new ServiceMoniker($"Razor.{logIdentifier}"));

            using var traceConfig = await LogHub.TraceConfiguration.CreateTraceConfigurationInstanceAsync(_serviceBroker, cancellationToken).ConfigureAwait(false);
            var traceSource = await traceConfig.RegisterLogSourceAsync(_logId, _logOptions, cancellationToken).ConfigureAwait(false);
            
            return traceSource;
        }

        public async Task<bool> TryInitializeServiceBrokerAsync(CancellationToken cancellationToken)
        {
            await _initializationSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Check if the service broker has already been initialized
                if (!(_serviceBroker is null))
                {
                    return true;
                }

                if (VSShell.Package.GetGlobalService(typeof(VSShell.Interop.SAsyncServiceProvider)) is not VSShell.IAsyncServiceProvider serviceProvider)
                {
                    return false;
                }

                var serviceContainer = await VSShell.ServiceExtensions.GetServiceAsync<
                    VSShell.ServiceBroker.SVsBrokeredServiceContainer,
                    VSShell.ServiceBroker.IBrokeredServiceContainer>(serviceProvider).ConfigureAwait(false);
                if (serviceContainer is null)
                {
                    return false;
                }

                _serviceBroker = serviceContainer.GetFullAccessServiceBroker();
                if (_serviceBroker is null)
                {
                    return false;
                }

                return true;
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }
    }
}
