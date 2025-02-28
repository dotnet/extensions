// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.TestUtilities;

/// <summary>Implements message bus to communicate tests skipped via SkipTestException.</summary>
public sealed class SkippedTestMessageBus : IMessageBus
{
    private readonly IMessageBus _innerBus;

    public SkippedTestMessageBus(IMessageBus innerBus)
    {
        _innerBus = innerBus;
    }

    public int SkippedTestCount { get; private set; }

    public void Dispose()
    {
        // nothing to dispose
    }

    public bool QueueMessage(IMessageSinkMessage message)
    {
        var testFailed = message as ITestFailed;

        if (testFailed != null)
        {
            var exceptionType = testFailed.ExceptionTypes.FirstOrDefault();
            if (exceptionType == typeof(SkipTestException).FullName)
            {
                SkippedTestCount++;
                return _innerBus.QueueMessage(new TestSkipped(testFailed.Test, testFailed.Messages.FirstOrDefault()));
            }
        }

        // Nothing we care about, send it on its way
        return _innerBus.QueueMessage(message);
    }
}
