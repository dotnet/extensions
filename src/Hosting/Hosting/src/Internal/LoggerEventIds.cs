// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.Internal
{
    internal static class LoggerEventIds
    {
        public static EventId Starting = new EventId(1, "Starting");
        public static EventId Started = new EventId(2, "Started");
        public static EventId Stopping = new EventId(3, "Stopping");
        public static EventId Stopped = new EventId(4, "Stopped");
        public static EventId StoppedWithException = new EventId(5, "StoppedWithException");
        public static EventId ApplicationStartupException = new EventId(6, "ApplicationStartupException");
        public static EventId ApplicationStoppingException = new EventId(7, "ApplicationStoppingException");
        public static EventId ApplicationStoppedException = new EventId(8, "ApplicationStoppedException");
    }
}
