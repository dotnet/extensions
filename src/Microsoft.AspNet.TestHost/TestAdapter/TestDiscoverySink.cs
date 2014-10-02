// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Framework.TestAdapter;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.TestHost
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
            Trace.TraceInformation("[TestDiscoverySink]: OnTransmit(TestDiscovery.TestFound)");
            _channel.Send(new Message
            {
                MessageType = "TestDiscovery.TestFound",
                Payload = JToken.FromObject(test),
            });
        }
    }
}