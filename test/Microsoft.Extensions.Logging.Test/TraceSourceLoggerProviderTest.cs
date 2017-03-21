// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET452
using System.Diagnostics;
using Microsoft.Extensions.Logging.TraceSource;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceLoggerProviderTest
    {
        [Fact]
        public void Dispose_TraceListenerIsFlushedOnce()
        {
            // Arrange
            var testSwitch = new SourceSwitch("TestSwitch", "Level will be set to warning for this test");
            testSwitch.Level = SourceLevels.Warning;
            var listener = new BufferedConsoleTraceListener();

            TraceSourceLoggerProvider provider = new TraceSourceLoggerProvider(testSwitch, listener);
            var logger1 = provider.CreateLogger("FirstLogger");
            var logger2 = provider.CreateLogger("SecondLogger");
            logger1.LogError("message1");
            logger2.LogError("message2");

            // Act
            provider.Dispose();

            // Assert
            Assert.Equal(1, listener.FlushCount);
        }

        private class BufferedConsoleTraceListener : ConsoleTraceListener
        {
            public int FlushCount { get; set; }

            public override void Flush()
            {
                FlushCount++;
                base.Flush();
            }
        }
    }
}
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif