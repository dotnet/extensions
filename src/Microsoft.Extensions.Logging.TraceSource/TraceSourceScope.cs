// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.TraceSource
{
    /// <summary>
    /// Provides an IDisposable that represents a logical operation scope based on System.Diagnostics LogicalOperationStack
    /// </summary>
    internal class TraceSourceScope : IDisposable
    {
        // To detect redundant calls
        private bool _isDisposed;

        /// <summary>
        /// Pushes state onto the LogicalOperationStack by calling 
        /// <see cref="Trace.CorrelationManager.StartLogicalOperation(object operationId)"/>
        /// </summary>
        /// <param name="state">The state.</param>
        public TraceSourceScope(object state)
        {
#if NET451
            Trace.CorrelationManager.StartLogicalOperation(state);
#endif
        }

        /// <summary>
        /// Pops a state off the LogicalOperationStack by calling
        /// <see cref="Trace.CorrelationManager.StopLogicalOperation()"/>
        /// </summary>
        /// <param name="state">The state.</param>
        public void Dispose()
        {
            if (!_isDisposed)
            {
#if NET451
                Trace.CorrelationManager.StopLogicalOperation();
#endif
                _isDisposed = true;
            }
        }
    }
}
