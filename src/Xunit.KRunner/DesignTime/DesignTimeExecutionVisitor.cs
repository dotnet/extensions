// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;

namespace Xunit.KRunner
{
    public class DesignTimeExecutionVisitor : TestMessageVisitor<ITestAssemblyFinished>
    {
        private readonly ITestExecutionSink _sink;

        public DesignTimeExecutionVisitor(ITestExecutionSink sink)
        {
            _sink = sink;
        }

        protected override bool Visit(ITestStarting testStarting)
        {
            _sink.RecordStart(testStarting.TestCase.ToDesignTimeTest());
            return base.Visit(testStarting);
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            var test = testSkipped.TestCase.ToDesignTimeTest();
            _sink.RecordResult(new TestResult(test)
            {
                Outcome = TestOutcome.Skipped,
            });

            return base.Visit(testSkipped);
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            var test = testFailed.TestCase.ToDesignTimeTest();
            var result = new TestResult(test)
            {
                Outcome = TestOutcome.Failed,

                Duration = TimeSpan.FromSeconds((double)testFailed.ExecutionTime),
                ErrorMessage = string.Join(Environment.NewLine, testFailed.Messages),
                ErrorStackTrace = string.Join(Environment.NewLine, testFailed.StackTraces),
            };

            result.Messages.Add(testFailed.Output);

            _sink.RecordResult(result);

            return base.Visit(testFailed);
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            var test = testPassed.TestCase.ToDesignTimeTest();
            _sink.RecordResult(new TestResult(test)
            {
                Outcome = TestOutcome.Passed,

                Duration = TimeSpan.FromSeconds((double)testPassed.ExecutionTime),
            });

            return base.Visit(testPassed);
        }
    }
}