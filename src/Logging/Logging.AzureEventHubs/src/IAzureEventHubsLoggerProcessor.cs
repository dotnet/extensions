// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.EventHubs;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    public interface IAzureEventHubsLoggerProcessor : IDisposable
    {
        void Process(EventData eventData);
    }
}
