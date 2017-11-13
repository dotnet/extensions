// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    internal static class EventIds
    {
        // Logging done by LoggingScopeHttpMessageHandler at INFO level - this surrounds the whole pipeline
        public static readonly EventId RequestPipelineStart = new EventId(100, "RequestPipelineStart");
        public static readonly EventId RequestPipelineEnd = new EventId(101, "RequestPipelineEnd");

        // Logging done by LoggingScopeHttpMessageHandler at DEBUG level
        public static readonly EventId RequestPipelineRequestHeader = new EventId(102, "RequestPipelineRequestHeader");
        public static readonly EventId RequestPipelineResponseHeader = new EventId(103, "RequestPipelineResponseHeader");

        // Logging done by LoggingHttpMessageHandler at INFO level - this surrounds the actual HTTP request/response
        public static readonly EventId ClientHandlerRequestStart = new EventId(100, "RequestStart");
        public static readonly EventId ClientHandlerRequestEnd = new EventId(101, "RequestEnd");

        // Logging done by LoggingHttpMessageHandler at DEBUG level
        public static readonly EventId ClientHandlerRequestHeader = new EventId(102, "RequestHeader");
        public static readonly EventId ClientHandlerResponseHeader = new EventId(103, "ResponseHeader");

    }
}
