// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    internal class DefaultAzureEventHubsLoggerProcessor : IAzureEventHubsLoggerProcessor
    {
        private readonly EventHubClient _eventHubClient;
        private readonly BlockingCollection<EventData> _queue;
        private readonly Task _worker;

        public DefaultAzureEventHubsLoggerProcessor(IOptions<AzureEventHubsLoggerOptions> options)
        {
            if (!string.IsNullOrWhiteSpace(options.Value.ConnectionString))
            {
                _eventHubClient = EventHubClient.CreateFromConnectionString(options.Value.ConnectionString);
            }
            else if (!string.IsNullOrWhiteSpace(options.Value.Namespace) && !string.IsNullOrWhiteSpace(options.Value.Instance))
            {
                var endpoint = new Uri($"sb://{options.Value.Namespace}.servicebus.windows.net");

                _eventHubClient = EventHubClient.CreateWithManagedServiceIdentity(endpoint, options.Value.Instance);
            }
            else
            {
                _eventHubClient = null;
            }

            _queue = new BlockingCollection<EventData>(1);
            _worker = Task.Factory.StartNew(ProcessQueueAsync,
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default);
        }

        public void Process(EventData eventData)
        {
            if (!_queue.IsAddingCompleted)
            {
                _queue.Add(eventData);
            }
        }

        private async Task ProcessQueueAsync()
        {
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                try
                {
                    await _eventHubClient.SendAsync(item);
                }
                catch
                {
                    // ignored
                }

                if (_queue.IsCompleted)
                {
                    return;
                }
            }
        }

        public void Dispose()
        {
            _queue.CompleteAdding();

            _worker.Wait();
        }
    }
}
