// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class TraceSourceScopeTest
    {
#if NET46
        [Fact]
        public static void DiagnosticsScope_PushesAndPops_LogicalOperationStack()
        {
            // Arrange
            var baseState = "base";
            Trace.CorrelationManager.StartLogicalOperation(baseState);
            var state = "1337state7331";

            var factory = new LoggerFactory();
            var logger = factory.CreateLogger("Test");
            factory.AddTraceSource(new SourceSwitch("TestSwitch"), new ConsoleTraceListener());

            // Act
            var a = Trace.CorrelationManager.LogicalOperationStack.Peek();
            var scope = logger.BeginScope(state);
            var b = Trace.CorrelationManager.LogicalOperationStack.Peek();
            scope.Dispose();
            var c = Trace.CorrelationManager.LogicalOperationStack.Peek();

            // Assert
            Assert.Same(a, c);
            Assert.Same(state, b);
        }
#elif NETCOREAPP2_0
#else
#error Target framework needs to be updated
#endif
    }
}
