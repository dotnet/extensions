// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.TestUtilities;

public class SkippedFactTestCase : XunitTestCase
{
    [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes", error: true)]
    public SkippedFactTestCase()
    {
    }

    public SkippedFactTestCase(
        IMessageSink diagnosticMessageSink, TestMethodDisplay defaultMethodDisplay, TestMethodDisplayOptions defaultMethodDisplayOptions,
        ITestMethod testMethod, object[]? testMethodArguments = null)
        : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
    {
    }

    public override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
                                                    IMessageBus messageBus,
                                                    object[] constructorArguments,
                                                    ExceptionAggregator aggregator,
                                                    CancellationTokenSource cancellationTokenSource)
    {
        using SkippedTestMessageBus skipMessageBus = new(messageBus);
        var result = await base.RunAsync(diagnosticMessageSink, skipMessageBus, constructorArguments, aggregator, cancellationTokenSource);
        if (skipMessageBus.SkippedTestCount > 0)
        {
            result.Failed -= skipMessageBus.SkippedTestCount;
            result.Skipped += skipMessageBus.SkippedTestCount;
        }

        return result;
    }
}
