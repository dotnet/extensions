using System;
using System.Diagnostics;
using Xunit;
#if NET45
using Moq;
#endif

namespace Microsoft.Framework.Logging.Test
{
    public class DiagnosticsScopeTests
    {
#if NET45
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