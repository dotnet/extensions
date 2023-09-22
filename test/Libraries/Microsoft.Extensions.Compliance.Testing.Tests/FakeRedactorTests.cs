// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Test;

public class FakeRedactorTests
{
    [Fact]
    public void Fake_Redactors_Shares_Behavior_Between_Instances_When_Different_Ctors_Used()
    {
        var r1 = new FakeRedactor();
        var r2 = FakeRedactor.Create();
        var r3 = FakeRedactor.Create(new FakeRedactorOptions());

        var data = "Ananas";

        var redacted1 = r1.Redact(data);
        var redacted2 = r2.Redact(data);
        var redacted3 = r3.Redact(data);

        Assert.Equal(redacted1, redacted2);
        Assert.Equal(redacted1, redacted3);
    }

    [Fact]
    public void Fake_Redactor_Throws_When_Provided_Redaction_Format_Is_Not_Correct()
    {
        Assert.Throws<ArgumentException>(() => FakeRedactor.Create(new FakeRedactorOptions { RedactionFormat = "{{{{{23123{}}" }));
    }

    [Fact]
    public void Can_Use_Fake_Redactor_With_Constant_Value()
    {
        const string RedactedConstOutput = "TEST";

        var redactor = FakeRedactor.Create(new FakeRedactorOptions { RedactionFormat = RedactedConstOutput });

        var redacted = redactor.Redact(new string('*', 100));

        Assert.Equal(RedactedConstOutput, redacted);
    }

    [Fact]
    public void When_Using_Factory_Method_To_Create_Without_Parameter_Fallback_Is_Used()
    {
        var data = "Bill Windmill";
        var redactor = FakeRedactor.Create();
        var redacted = redactor.Redact(data);

        Assert.Equal(redacted, data);
    }

    /// <summary>
    /// We are using singleton fake redactor using parallel for each which simulates parallel workload.
    /// If the classes are thread safe we should never get the same sequence number assigned to two events.
    /// We should also always have the same number of results - it will mean that we never overwrote any data.
    /// </summary>
    [Fact]
    public void FakeRedaction_EventTracking_Is_Thread_Safe()
    {
        var fakeRedactorProvider = new FakeRedactorProvider();

        var iterations = new int[30];
        for (var i = 0; i < iterations.Length; i++)
        {
            iterations[i] = i;
        }

        var dc = new[]
        {
            new DataClassification("TAX", 1),
            new DataClassification("TAX", 2),
            new DataClassification("TAX", 3),
        };

        Parallel.ForEach(iterations, iteration =>
        {
            var r = fakeRedactorProvider.GetRedactor(dc[iteration % dc.Length]);
            r.Redact(iteration.ToString(CultureInfo.InvariantCulture));
        });

        Assert.Equal(fakeRedactorProvider.Collector.AllRedactorRequests.Count, iterations.Length);

        var sequenceNumbersRedacted = fakeRedactorProvider.Collector.AllRedactedData.Select(x => x.SequenceNumber);
        var sequenceNumbersRequested = fakeRedactorProvider.Collector.AllRedactorRequests.Select(x => x.SequenceNumber);

        var numbersSetRedacted = new HashSet<int>(sequenceNumbersRedacted);
        var numbersSetRequested = new HashSet<int>(sequenceNumbersRequested);

        Assert.Equal(numbersSetRedacted.Count, iterations.Length);
        Assert.Equal(numbersSetRequested.Count, iterations.Length);
    }
}
