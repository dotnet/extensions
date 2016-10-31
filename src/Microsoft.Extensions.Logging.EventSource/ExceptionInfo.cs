// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// Represents information about exceptions that is captured by EventSourceLogger
    /// </summary>
#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
    [System.Diagnostics.Tracing.EventData(Name ="ExceptionInfo")]
#endif
    internal class ExceptionInfo
    {
        public string TypeName { get; set; }
        public string Message { get; set; }
        public int HResult { get; set; }
        public string VerboseMessage { get; set; } // This is the ToString() of the Exception
    }
}
