// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Logging.Testing;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class SensitiveRecordTests
{
    [Fact]
    public void TestRecordInTemplate()
    {
        using var logger = Utils.GetLogger();
        SensitiveRecordExtensions.LogInTemplate(logger, new());

        var logRecord = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, logRecord.Message);
        Assert.NotNull(logRecord.StructuredState);
        Assert.All(logRecord.StructuredState, x => Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, x.Value));
    }

    [Fact]
    public void TestRecordStructured()
    {
        using var logger = Utils.GetLogger();
        SensitiveRecordExtensions.LogFullyStructured(logger, new());

        var logRecord = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, logRecord.Message);
        Assert.NotNull(logRecord.StructuredState);
        Assert.All(logRecord.StructuredState, x => Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, x.Value));
    }

    [Fact]
    public void TestSensitiveRecordWithLogPropsAndTemplate()
    {
        using var logger = Utils.GetLogger();
        var dataToLog = new SensitiveRecordExtensions.RecordWithSensitiveMembers { PropGetSet = SensitiveRecordExtensions.Sensitive };
        SensitiveRecordExtensions.LogPropertiesWithTemplate(logger, dataToLog);

        var logRecord = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, logRecord.Message);
        Assert.NotNull(logRecord.StructuredState);
        Assert.All(logRecord.StructuredState, x => Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, x.Value));
    }

    [Fact]
    public void TestSensitiveRecordWithLogPropsNoTemplate()
    {
        using var logger = Utils.GetLogger();
        var dataToLog = new SensitiveRecordExtensions.RecordWithSensitiveMembers { PropGetSet = SensitiveRecordExtensions.Sensitive };
        SensitiveRecordExtensions.LogPropertiesFullyStructured(logger, dataToLog);

        var logRecord = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, logRecord.Message);
        Assert.NotNull(logRecord.StructuredState);
        Assert.All(logRecord.StructuredState, x => Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, x.Value));
    }

    [Fact]
    public void TestSensitiveRecordWithInlineAnnotation()
    {
        using var logger = Utils.GetLogger();
        var dataToLog = new SensitiveRecordExtensions.RecordWithSensitiveMembers { PropGetSet = SensitiveRecordExtensions.Sensitive };
        SensitiveRecordExtensions.LogInTemplateWithAnnotation(logger, dataToLog);

        var logRecord = Assert.Single(logger.FakeLogCollector.GetSnapshot());
        Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, logRecord.Message);
        Assert.NotNull(logRecord.StructuredState);
        Assert.All(logRecord.StructuredState, x => Assert.DoesNotContain(SensitiveRecordExtensions.Sensitive, x.Value));
    }
}
