// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Dnx.TestAdapter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Dnx.TestHost
{
    public class TestDiscoverySink : ITestDiscoverySink
    {
        private readonly ReportingChannel _channel;

        public TestDiscoverySink(ReportingChannel channel)
        {
            _channel = channel;
        }

        public void SendTest(Test test)
        {
            _channel.Send(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            });
        }
    }
}