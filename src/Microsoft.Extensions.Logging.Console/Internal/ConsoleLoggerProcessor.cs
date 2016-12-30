// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.Console.Internal
{
    public class ConsoleLoggerProcessor
    {
        private const int _maxQueuedMessages = 1024;

        private IConsole _console;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly ConcurrentQueue<LogMessageEntry> _messageQueue = new ConcurrentQueue<LogMessageEntry>();
        private readonly Task _outputTask;

        private readonly ManualResetEventSlim _backpressure = new ManualResetEventSlim(true);
        private readonly object _countLock = new object();

        private int _queuedMessageCount;
        private bool _isShuttingDown = false;

        public ConsoleLoggerProcessor()
        {
            RegisterForExit();

            // Start Console message queue processor
            _outputTask = Task.Factory.StartNew(
                ProcessLogQueue,
                this,
                TaskCreationOptions.LongRunning);
        }

        public IConsole Console
        {
            get { return _console; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _console = value;
            }
        }

        public bool HasQueuedMessages => !_messageQueue.IsEmpty;

        public void EnqueueMessage(LogMessageEntry message)
        {
            ApplyBackpressure();

            _messageQueue.Enqueue(message);

            WakeupProcessor();
        }

        private void ProcessLogQueue()
        {
            bool isShuttingDown;
            do
            {
                isShuttingDown = WaitForNewMessages();

                OutputQueuedMessages();

            } while (!isShuttingDown);
        }

        private bool WaitForNewMessages()
        {
            if (_messageQueue.IsEmpty && !Volatile.Read(ref _isShuttingDown))
            {
                // No messages; wait for new messages
                _semaphore.Wait();
            }

            return Volatile.Read(ref _isShuttingDown);
        }

        private void OutputQueuedMessages()
        {
            var messagesOutput = 0;
            LogMessageEntry message;
            while (_messageQueue.TryDequeue(out message))
            {
                if (message.LevelString != null)
                {
                    Console.Write(message.LevelString, message.LevelBackground, message.LevelForeground);
                }

                Console.Write(message.Message, message.MessageColor, message.MessageColor);
                messagesOutput++;
            }

            if (messagesOutput > 0)
            {
                // In case of AnsiLogConsole, the messages are not yet written to the console, flush them
                Console.Flush();

                ReleaseBackpressure(messagesOutput);
            }
        }

        private void WakeupProcessor()
        {
            if (_semaphore.CurrentCount == 0)
            {
                // Console output Task may be asleep, wake it up
                _semaphore.Release();
            }
        }

        private void ApplyBackpressure()
        {
            do
            {
                // Check if back pressure applied
                _backpressure.Wait();

                lock (_countLock)
                {
                    var messageCount = _queuedMessageCount + 1;
                    if (messageCount <= _maxQueuedMessages)
                    {
                        _queuedMessageCount = messageCount;
                        if (messageCount == _maxQueuedMessages)
                        {
                            // Next message would put the queue over max, set blocking
                            _backpressure.Reset();
                        }

                        // Exit and queue message
                        break;
                    }
                }

            } while (true);
        }

        private void ReleaseBackpressure(int messagesOutput)
        {
            lock (_countLock)
            {
                if (_queuedMessageCount >= _maxQueuedMessages &&
                    _queuedMessageCount - messagesOutput < _maxQueuedMessages)
                {
                    // Was blocked, unblock
                    _backpressure.Set();
                }
                _queuedMessageCount -= messagesOutput;
            }
        }

        private static void ProcessLogQueue(object state)
        {
            var consoleLogger = (ConsoleLoggerProcessor)state;

            consoleLogger.ProcessLogQueue();
        }

        private void RegisterForExit()
        {
            // Hooks to detect Process exit, and allow the Console to complete output
#if NET451
            AppDomain.CurrentDomain.ProcessExit += InitiateShutdown;
#elif NETSTANDARD1_5
            var currentAssembly = typeof(ConsoleLogger).GetTypeInfo().Assembly;
            System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(currentAssembly).Unloading += InitiateShutdown;
#endif
        }

#if NET451
        private void InitiateShutdown(object sender, EventArgs e)
#elif NETSTANDARD1_5
        private void InitiateShutdown(System.Runtime.Loader.AssemblyLoadContext obj)
#else
        private void InitiateShutdown()
#endif
        {
            _isShuttingDown = true;
            _semaphore.Release(); // Fast wake up vs cts
            try
            {
                _outputTask.Wait(1500); // with timeout in-case Console is locked by user input
            }
            catch (TaskCanceledException) { }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }
        }
    }
}
