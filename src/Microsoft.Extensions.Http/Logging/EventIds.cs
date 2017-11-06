// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Http.Logging
{
    internal static class EventIds
    {
        // Logging done by LoggingScopeHttpMessageHandler - this surrounds the whole pipeline
        public static readonly EventId RequestPipelineStart = new EventId(100, "RequestPipelineStart");
        public static readonly EventId RequestPipelineEnd = new EventId(101, "RequestPipelineEnd");

        // Logging done by LoggingHttpMessageHandler - this surrounds the actual HTTP request/response
        public static readonly EventId RequestStart = new EventId(102, "RequestStart");
        public static readonly EventId RequestEnd = new EventId(103, "RequestEnd");
    }
}
