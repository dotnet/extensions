// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Framework.TestAdapter;
using Newtonsoft.Json.Linq;

namespace Microsoft.Framework.TestHost
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
            Trace.TraceInformation("[TestExecutionSink]: OnTransmit(TestExecution.TestResult)");
            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestResult",
                Payload = JToken.FromObject(testResult),
            });
        }

        public void RecordStart(Test test)
        {
            Trace.TraceInformation("[TestExecutionSink]: OnTransmit(TestExecution.TestStarted)");
            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            });
        }
    }
}