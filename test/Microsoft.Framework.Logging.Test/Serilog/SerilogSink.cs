// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Framework.Logging.Test.Serilog
{
    public class SerilogSink : ILogEventSink
    {
        public List<LogEvent> Writes { get; set; } = new List<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            Writes.Add(logEvent);
        }
    }
}