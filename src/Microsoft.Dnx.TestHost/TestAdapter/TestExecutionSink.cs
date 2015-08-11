// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Dnx.TestAdapter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Dnx.TestHost
{
    public class TestExecutionSink : ITestExecutionSink
    {
        private readonly ReportingChannel _channel;

        public TestExecutionSink(ReportingChannel channel)
        {
            _channel = channel;
        }

        public void RecordResult(TestResult testResult)
        {
            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestResult",
                Payload = JToken.FromObject(testResult),
            });
        }

        public void RecordStart(Test test)
        {
            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            });
        }
    }
}