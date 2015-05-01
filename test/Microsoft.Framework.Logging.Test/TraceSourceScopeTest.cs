// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Xunit;
#if DNX451
using Moq;
#endif
using Microsoft.Framework.Logging.Internal;

namespace Microsoft.Framework.Logging.Test
{
    public class TraceSourceScopeTest
    {
#if DNX451
        [Fact]
        public static void DiagnosticsScope_PushesAndPops_LogicalOperationStack()
        {
            // Arrange
            var baseState = "base";
            Trace.CorrelationManager.StartLogicalOperation(baseState);
            var state = "1337state7331";

            // Act
            var a = Trace.CorrelationManager.LogicalOperationStack.Peek();
            var scope = new TraceSourceScope(state);
            var b = Trace.CorrelationManager.LogicalOperationStack.Peek();
            scope.Dispose();
            var c = Trace.CorrelationManager.LogicalOperationStack.Peek();

            // Assert
            Assert.Same(a, c);
            Assert.Same(state, b);
        }
#endif
    }
}
