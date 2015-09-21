// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Dnx.Testing.Abstractions;
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
            if (testResult == null)
            {
                throw new ArgumentNullException(nameof(testResult));
            }

            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestResult",
                Payload = JToken.FromObject(testResult),
            });
        }

        public void RecordStart(Test test)
        {
            if (test == null)
            {
                throw new ArgumentNullException(nameof(test));
            }

            _channel.Send(new Message
            {
                MessageType = "TestExecution.TestStarted",
                Payload = JToken.FromObject(test),
            });
        }
    }
}