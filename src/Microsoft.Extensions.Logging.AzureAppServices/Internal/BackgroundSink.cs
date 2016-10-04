// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Serilog.Core;
using Serilog.Events;

// TODO: Might want to consider using https://github.com/jezzsantos/Serilog.Sinks.Async 
// instead of this, once that supports netstandard
namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <summary>
    /// A background sink for Serilog.
    /// </summary>
    public class BackgroundSink : ILogEventSink, IDisposable
    {
        /// <summary>
        /// The default queue size.
        /// </summary>
        public const int DefaultLogMessagesQueueSize = 1024;

        private readonly CancellationTokenSource _disposedTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _disposedToken;

        private readonly BlockingCollection<LogEvent> _messages;
        private readonly Thread _workerThread;

        private ILogEventSink _innerSink;

        /// <summary>
        /// Creates a new instance of the <see cref="BackgroundSink"/> class.
        /// </summary>
        /// <param name="innerSink">The inner sink which does the actual logging</param>
        /// <param name="maxQueueSize">The maximum size of the background queue</param>
        public BackgroundSink(ILogEventSink innerSink, int? maxQueueSize)
        {
            if (innerSink == null)
            {
                throw new ArgumentNullException(nameof(innerSink));
            }

            _disposedToken = _disposedTokenSource.Token;

            if (maxQueueSize == null || maxQueueSize <= 0)
            {
                _messages = new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>());
            }
            else
            {
                _messages = new BlockingCollection<LogEvent>(new ConcurrentQueue<LogEvent>(), maxQueueSize.Value);
            }

            _innerSink = innerSink;

            _workerThread = new Thread(Worker);
            _workerThread.Name = GetType().Name;
            _workerThread.IsBackground = true;
            _workerThread.Start();
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            if (!_disposedToken.IsCancellationRequested)
            {
                _messages.Add(logEvent);
            }
        }

        /// <summary>
        /// Disposes this object instance.
        /// </summary>
        public virtual void Dispose()
        {
            lock (_disposedTokenSource)
            {
                if (!_disposedTokenSource.IsCancellationRequested)
                {
                    _disposedTokenSource.Cancel();
                }

                // Wait for the thread to complete before disposing the resources
                _workerThread.Join(5 /*seconds */ * 1000);
                _messages.Dispose();
            }
        }

        private void Worker()
        {
            try
            {
                foreach (var logEvent in _messages.GetConsumingEnumerable(_disposedToken))
                {
                    PassLogEventToInnerSink(logEvent);
                }
            }
            catch (OperationCanceledException)
            {
                // Do nothing, we just cancelled the task
            }
        }

        private void PassLogEventToInnerSink(LogEvent logEvent)
        {
            _innerSink.Emit(logEvent);
        }
    }
}
