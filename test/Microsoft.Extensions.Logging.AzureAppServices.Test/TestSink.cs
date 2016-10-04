// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    internal class TestSink : ILogEventSink
    {
        private readonly ObservableCollection<LogEvent> _events = new ObservableCollection<LogEvent>();

        public ObservableCollection<LogEvent> Events => _events;

        public Action<LogEvent> Filter { get; set; }

        public void Emit(LogEvent logEvent)
        {
            Filter?.Invoke(logEvent);
            _events.Add(logEvent);
        }
    }
}
