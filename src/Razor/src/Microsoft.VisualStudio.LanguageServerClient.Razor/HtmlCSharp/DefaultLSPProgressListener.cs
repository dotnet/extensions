// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPProgressListener))]
    internal class DefaultLSPProgressListener : LSPProgressListener, IDisposable
    {
        private const string ProgressNotificationValueName = "value";

        private readonly ILanguageServiceBroker2 _languageServiceBroker;

        private readonly ConcurrentDictionary<string, ProgressRequest> _activeRequests
            = new ConcurrentDictionary<string, ProgressRequest>();

        [ImportingConstructor]
        public DefaultLSPProgressListener(ILanguageServiceBroker2 languageServiceBroker)
        {
            if (languageServiceBroker is null)
            {
                throw new ArgumentNullException(nameof(languageServiceBroker));
            }

            _languageServiceBroker = languageServiceBroker;

            _languageServiceBroker.ClientNotifyAsync += ClientNotifyAsyncListenerAsync;
        }

        public override bool TryListenForProgress(
            string token,
            Func<JToken, CancellationToken, Task> onProgressNotifyAsync,
            Func<CancellationToken, Task> delayAfterLastNotifyAsync,
            CancellationToken handlerCancellationToken,
            out Task onCompleted)
        {
            var onCompletedSource = new TaskCompletionSource<bool>();
            var request = new ProgressRequest(
                onProgressNotifyAsync,
                delayAfterLastNotifyAsync,
                handlerCancellationToken,
                onCompletedSource);

            if (!_activeRequests.TryAdd(token, request))
            {
                onCompleted = null;
                return false;
            }

            CompleteAfterDelay(token, request);
            onCompleted = onCompletedSource.Task;
            return true;
        }

        private Task ClientNotifyAsyncListenerAsync(object sender, LanguageClientNotifyEventArgs args)
            => ProcessProgressNotificationAsync(args.MethodName, args.ParameterToken);

        // Internal for testing
        internal async Task ProcessProgressNotificationAsync(string methodName, JToken parameterToken)
        {
            if (methodName != Methods.ProgressNotificationName ||
               !parameterToken.HasValues ||
               parameterToken[ProgressNotificationValueName] is null ||
               parameterToken[Methods.ProgressNotificationTokenName] is null)
            {
                return;
            }

            var token = parameterToken[Methods.ProgressNotificationTokenName].ToObject<string>(); // IProgress<object>>();

            if (string.IsNullOrEmpty(token) || !_activeRequests.TryGetValue(token, out var request))
            {
                return;
            }

            var value = parameterToken[ProgressNotificationValueName];

            try
            {
                request.HandlerCancellationToken.ThrowIfCancellationRequested();
                await request.OnProgressNotifyAsync(value, request.HandlerCancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                // Initial handle request has been cancelled
                // We can deem this ProgressRequest complete
                ProgressCompleted(token);
                return;
            }

            CompleteAfterDelay(token, request);
        }

        private void CompleteAfterDelay(string token, ProgressRequest request)
        {
            CancellationTokenSource linkedCTS;

            lock (request.RequestLock)
            {
                CancelAndDisposeToken(request.TimeoutCancellationTokenSource);

                request.TimeoutCancellationTokenSource = new CancellationTokenSource();
                linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(
                    request.TimeoutCancellationTokenSource.Token,
                    request.HandlerCancellationToken);
            }

            _ = CompleteAfterDelayAsync(token, request.DelayAfterLastNotifyAsync, linkedCTS); // Fire and forget


            async Task CompleteAfterDelayAsync(string token, Func<CancellationToken, Task> delayAfterLastNotifyAsync, CancellationTokenSource cts)
            {
                try
                {
                    await delayAfterLastNotifyAsync(cts.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    // Task cancelled, new progress notification received.
                    // Don't allow handler to return
                    return;
                }
                finally
                {
                    // Dispose of the Linked Cancellation Token Source
                    // Note: The component TimeoutCancellationTokenSource & HandlerCancellationToken
                    //       should not be impacted. Their lifecycle is managed using CancelAndDisposeToken
                    //       and the platform handler, respectively.
                    cts.Dispose();
                }

                ProgressCompleted(token);
            }
        }

        private static void CancelAndDisposeToken(CancellationTokenSource cts)
        {
            if (cts != null &&
                cts.Token.CanBeCanceled &&
                !cts.Token.IsCancellationRequested)
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        private void ProgressCompleted(string token)
        {
            if (_activeRequests.TryRemove(token, out var request))
            {
                // We're setting the result of a Task<bool>
                // however we only return a Task to the
                // handler/subscriber, so the bool is ignored
                request.OnCompleted.SetResult(false);
                CancelAndDisposeToken(request.TimeoutCancellationTokenSource);
            }
        }

        public void Dispose()
        {
            _languageServiceBroker.ClientNotifyAsync -= ClientNotifyAsyncListenerAsync;

            foreach (var token in _activeRequests.Keys)
            {
                ProgressCompleted(token);
            }
        }

        private class ProgressRequest
        {
            public ProgressRequest(
                Func<JToken, CancellationToken, Task> onProgressNotifyAsync,
                Func<CancellationToken, Task> delayAfterLastNotifyAsync,
                CancellationToken handlerCancellationToken,
                TaskCompletionSource<bool> onCompleted)
            {
                if (onProgressNotifyAsync is null)
                {
                    throw new ArgumentNullException(nameof(onProgressNotifyAsync));
                }

                if (onCompleted is null)
                {
                    throw new ArgumentNullException(nameof(onCompleted));
                }

                if (delayAfterLastNotifyAsync is null)
                {
                    throw new ArgumentNullException(nameof(delayAfterLastNotifyAsync));
                }

                OnProgressNotifyAsync = onProgressNotifyAsync;
                DelayAfterLastNotifyAsync = delayAfterLastNotifyAsync;
                HandlerCancellationToken = handlerCancellationToken;
                OnCompleted = onCompleted;
            }

            internal Func<JToken, CancellationToken, Task> OnProgressNotifyAsync { get; }
            internal TaskCompletionSource<bool> OnCompleted { get; }
            internal CancellationToken HandlerCancellationToken { get; }

            internal Func<CancellationToken, Task> DelayAfterLastNotifyAsync { get; }
            internal CancellationTokenSource TimeoutCancellationTokenSource { get; set; }
            internal object RequestLock { get; } = new object();
        }
    }
}
