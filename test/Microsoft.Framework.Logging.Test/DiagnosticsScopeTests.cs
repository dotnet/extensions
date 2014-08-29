// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Xunit;
#if ASPNET50
using Moq;
#endif

namespace Microsoft.Framework.Logging.Test
{
    public class DiagnosticsScopeTests
    {
#if ASPNET50
        [Fact]
        public static void DiagnosticsScope_PushesAndPops_LogicalOperationStack()
        {
            // Arrange
            var baseState = "base";
            Trace.CorrelationManager.StartLogicalOperation(baseState);
            var state = "1337state7331";

            // Act
            var a = Trace.CorrelationManager.LogicalOperationStack.Peek();
            var scope = new DiagnosticsScope(state);
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