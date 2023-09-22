// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Test;

public class RedactionFakesAcceptanceTests
{
    [Fact]
    public void Can_Register_And_Use_Fake_Redactor_With_Default_Options_With_DataClassification()
    {
        var dc = new DataClassification("Foo", 0x1);

        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetFakeRedactor(dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Contains(data, redacted);
    }

    [Fact]
    public void Can_Register_And_Use_Fake_Redactor_With_Default_Options()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetFakeRedactor(dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Contains(data, redacted);
    }

    [Fact]
    public void Can_Register_And_Use_Fake_Redactor_With_Configuration_Section_Options_With_Data_Classification()
    {
        var dc = new DataClassification("Foo", 0x1);

        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetFakeRedactor(Setup.GetFakesConfiguration(), dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Contains(data, redacted);
    }

    [Fact]
    public void Can_Register_And_Use_Fake_Redactor_With_Configuration_Section_Options()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetFakeRedactor(Setup.GetFakesConfiguration(), dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Contains(data, redacted);
    }

    [Fact]
    public void Can_Register_And_Use_Fake_Redactor_With_Action_Options_With_DataClassification()
    {
        var dc = new DataClassification("Foo", 0x1);

        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetFakeRedactor(x => { x.RedactionFormat = "xxx{0}xxx"; }, dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Contains(data, redacted);
    }

    [Fact]
    public void AddRedactionAndSetFakeRedactor_Pick_Up_Options_Correctly()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction()
            .AddRedaction(builder => builder.SetFakeRedactor(options => { options.RedactionFormat = "xxx{0}xxx"; }, dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Equal($"xxx{data}xxx", redacted);
    }

    [Fact]
    public void AddRedactionWithActionAndSetFakeRedactor_Pick_Up_Options_Correctly()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction(_ => { })
            .AddRedaction(builder => builder.SetFakeRedactor(options => { options.RedactionFormat = "xxx{0}xxx"; }, dc))
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Equal($"xxx{data}xxx", redacted);
    }

    [Fact]
    public void SetFakeRedactorAndAddRedaction_Pick_Up_Options_Correctly()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(builder => builder.SetFakeRedactor(options => { options.RedactionFormat = "xxx{0}xxx"; }, dc))
            .AddFakeRedaction()
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Equal($"xxx{data}xxx", redacted);
    }

    [Fact]
    public void SetFakeRedactorAndAddRedactionWithAction_Pick_Up_Options_Correctly()
    {
        var dc = new DataClassification("TAX", 1);
        var data = "Lalaalal";
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(builder => builder.SetFakeRedactor(options => { options.RedactionFormat = "xxx{0}xxx"; }, dc))
            .AddFakeRedaction(_ => { })
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();

        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);

        Assert.IsAssignableFrom<FakeRedactor>(r);
        Assert.Equal($"xxx{data}xxx", redacted);
    }

    [Fact]
    public void RedactionFakesEventCollector_Can_Be_Obtained_From_DI_And_Show_Redaction_History()
    {
        var data = "Lalaalal";
        var data2 = "Lalaalal222222222";
        var redactionFormat = "xxx";

        using var services = new ServiceCollection()
            .AddLogging()
            .AddFakeRedaction(x => x.RedactionFormat = redactionFormat)
            .BuildServiceProvider();

        var provider = services.GetRequiredService<IRedactorProvider>();
        var collector = services.GetFakeRedactionCollector();

        var dc = new DataClassification("TAX", 1);
        var r = provider.GetRedactor(dc);
        var redacted = r.Redact(data);
        var redacted2 = r.Redact(data2);

        Assert.Equal(2, collector.AllRedactedData.Count);
        Assert.Equal(data2, collector.LastRedactedData.Original);
        Assert.Equal(redacted2, collector.LastRedactedData.Redacted);
        Assert.Equal(2, collector.LastRedactedData.SequenceNumber);

        Assert.Equal(1, collector.AllRedactedData[0].SequenceNumber);
        Assert.Equal(data, collector.AllRedactedData[0].Original);
        Assert.Equal(redacted, collector.AllRedactedData[0].Redacted);

        Assert.Equal(1, collector.LastRedactorRequested.SequenceNumber);
        Assert.Equal(dc, collector.LastRedactorRequested.DataClassification);
        Assert.Equal(1, collector.AllRedactorRequests.Count);
    }

    [Fact]
    public void Fake_Redaction_Extensions_Does_Not_Allow_Null_Arguments()
    {
        var dc = new DataClassification("TAX", 1);

        Assert.Throws<ArgumentNullException>(() => ((IRedactionBuilder)null!).SetFakeRedactor(dc));
        Assert.Throws<ArgumentNullException>(() => ((IRedactionBuilder)null!).SetFakeRedactor(Setup.GetFakesConfiguration(), dc));
        Assert.Throws<ArgumentNullException>(() => ((IRedactionBuilder)null!).SetFakeRedactor(x => x.RedactionFormat = "2", dc));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddRedaction(x => x.SetFakeRedactor((Action<FakeRedactorOptions>)null!, dc)));

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRedaction(x => x.SetFakeRedactor((Action<FakeRedactorOptions>)null!, dc)));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeRedaction());
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddFakeRedaction(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddFakeRedaction(null!));
    }

    [Fact]
    public void Fake_Redaction_Works_Fine_Without_Any_Config()
    {
        using var sp = new ServiceCollection().AddFakeRedaction().BuildServiceProvider();

        var rp = sp.GetRequiredService<IRedactorProvider>();
        var dc = new DataClassification("TAX", 1);
        var r = rp.GetRedactor(dc);
        var collector = sp.GetFakeRedactionCollector();

        var redacted = r.Redact("dddd");

        Assert.Equal(dc, collector.LastRedactorRequested.DataClassification);
        Assert.Equal(redacted, collector.LastRedactedData.Redacted);
    }
}
