// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// A generic interface for logging.
    /// </summary>
#if K10
    [Runtime.AssemblyNeutral]
#endif
    public interface ILogger
    {
        /// <summary>
        /// Aggregates most logging patterns to a single method.  This must be compatible with the Func representation in the OWIN environment.
        /// 
        /// To check IsEnabled call WriteCore with only TraceEventType and check the return value, no event will be written.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter);


        /// <summary>
        /// Begins a logical operation scope.
        /// </summary>
        /// <param name="state">The identifier for the scope.</param>
        /// <returns>An IDisposable that ends the logical operation scope on dispose.</returns>
        IDisposable BeginScope(object state);
    }
}
