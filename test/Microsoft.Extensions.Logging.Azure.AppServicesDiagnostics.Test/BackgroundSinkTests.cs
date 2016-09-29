// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Serilog;
using Xunit;

namespace Microsoft.Extensions.Logging.Azure.AppServicesDiagnostics.Test
{
    public class BackgroundSinkTests
    {
        private readonly int DefaultTimeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

        [Fact]
        public void MessagesOrderIsMaintained()
        {
            var testSink = new TestSink();

            using (var allLogged = new ManualResetEvent(false))
            using (var backgroundSink = new BackgroundSink(testSink, BackgroundSink.DefaultLogMessagesQueueSize))
            {
                testSink.Events.CollectionChanged += (sender, e) =>
                {
                    if (testSink.Events.Count >= 3)
                    {
                        allLogged.Set();
                    }
                };

                var logger = new LoggerConfiguration()
                   .WriteTo.Sink(backgroundSink)
                   .CreateLogger();

                logger.Information("5");
                logger.Information("1");
                logger.Information("3");

                Assert.True(allLogged.WaitOne(DefaultTimeout));

                var eventsText = testSink.Events.Select(e => e.MessageTemplate.Text).ToArray();

                Assert.Equal("5", eventsText[0]);
                Assert.Equal("1", eventsText[1]);
                Assert.Equal("3", eventsText[2]);
            }
        }

        [Fact]
        public void BlocksWhenQueueIsFull()
        {
            using (var unblockEvent = new ManualResetEvent(false))
            {
                var testSink = new TestSink
                {
                    // Block inner logging write (simulates slow writes)
                    // When combined with a limited size queue, it will
                    // be like having more logs than it can process
                    Filter = ev =>
                    {
                        unblockEvent.WaitOne(DefaultTimeout);
                    }
                };

                using (var allLogged = new ManualResetEvent(false))
                using (var backgroundSink = new BackgroundSink(testSink, maxQueueSize: 1))
                {
                    testSink.Events.CollectionChanged += (sender, e) =>
                    {
                        if (testSink.Events.Count >= 3)
                        {
                            allLogged.Set();
                        }
                    };

                    var logger = new LoggerConfiguration()
                       .WriteTo.Sink(backgroundSink)
                       .CreateLogger();

                    logger.Information("7");
                    logger.Information("3");

                    var secondLogTask = Task.Run(() =>
                    {
                        logger.Information("1");
                    });

                    // There should be no events written while the queue is blocked
                    // and no more logs should be added
                    var logWasUnblocked = secondLogTask.Wait(DefaultTimeout / 10);
                    var sinkHasEvents = testSink.Events.Any();

                    // Now unblock and wait for all events to flush
                    unblockEvent.Set();

                    // Postpone the assert until after we unblock the event
                    // otherwise xunit will hang because it blocks the test thread
                    Assert.False(logWasUnblocked);
                    Assert.False(sinkHasEvents);

                    Assert.True(allLogged.WaitOne(DefaultTimeout));

                    var eventsText = testSink.Events.Select(e => e.MessageTemplate.Text).ToArray();

                    Assert.Equal("7", eventsText[0]);
                    Assert.Equal("3", eventsText[1]);
                    Assert.Equal("1", eventsText[2]);
                }
            }
        }
    }
}
