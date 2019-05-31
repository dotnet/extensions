// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.EventHubs;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    public interface IAzureEventHubsLoggerFormatter
    {
        EventData Format<TState>(LogLevel logLevel, string name, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter, IExternalScopeProvider scopeProvider);
    }
}
