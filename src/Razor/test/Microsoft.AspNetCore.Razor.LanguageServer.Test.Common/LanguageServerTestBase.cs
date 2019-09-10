// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common
{
    public abstract class LanguageServerTestBase
    {
        public LanguageServerTestBase()
        {
            Dispatcher = new SingleThreadedForegroundDispatcher();
            LoggerFactory = Mock.Of<ILoggerFactory>(factory => factory.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>());
        }

        internal ForegroundDispatcher Dispatcher { get; }

        protected ILoggerFactory LoggerFactory { get; }

        private class SingleThreadedForegroundDispatcher : ForegroundDispatcher
        {
            public SingleThreadedForegroundDispatcher()
            {
                ForegroundScheduler = SynchronizationContext.Current == null ? new ThrowingTaskScheduler() : TaskScheduler.FromCurrentSynchronizationContext();
                BackgroundScheduler = TaskScheduler.Default;
            }

            public override TaskScheduler ForegroundScheduler { get; }

            public override TaskScheduler BackgroundScheduler { get; }

            private Thread Thread { get; } = Thread.CurrentThread;

            public override bool IsForegroundThread => Thread.CurrentThread == Thread;
        }

        private class ThrowingTaskScheduler : TaskScheduler
        {
            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }

            protected override void QueueTask(Task task)
            {
                throw new NotImplementedException();
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                throw new NotImplementedException();
            }
        }
    }
}
