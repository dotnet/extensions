// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public class RedactorProviderTests
{
    [Fact]
    public void RedactorProvider_Returns_Redactor_For_Every_Data_Classification()
    {
        var dc = new DataClassification("Foo", "0x2");

        var redactorProvider = new RedactorProvider(
            redactors: new Redactor[] { ErasingRedactor.Instance, NullRedactor.Instance },
            options: Microsoft.Extensions.Options.Options.Create(new RedactorProviderOptions()));

        var r = redactorProvider.GetRedactor(dc);
        Assert.IsAssignableFrom<Redactor>(r);
    }

    private static readonly DataClassification _dataClassification1 = new("TAX", "1");
    private static readonly DataClassification _dataClassification2 = new("TAX", "2");
    private static readonly DataClassification _dataClassification3 = new("TAX", "4");

    [Fact]
    public void RedactorProvider_Returns_Redactor_For_Data_Classifications()
    {
        var opt = new RedactorProviderOptions();
        opt.Redactors.Add(_dataClassification1, typeof(ErasingRedactor));
        opt.Redactors.Add(_dataClassification2, typeof(NullRedactor));

        var redactorProvider = new RedactorProvider(
            redactors: new Redactor[] { ErasingRedactor.Instance, NullRedactor.Instance },
            options: Microsoft.Extensions.Options.Options.Create(opt));

        var r1 = redactorProvider.GetRedactor(_dataClassification1);
        var r2 = redactorProvider.GetRedactor(_dataClassification2);
        var r3 = redactorProvider.GetRedactor(_dataClassification3);

        Assert.Equal(typeof(ErasingRedactor), r1.GetType());
        Assert.Equal(typeof(NullRedactor), r2.GetType());
        Assert.Equal(typeof(ErasingRedactor), r3.GetType());
    }

    [Fact]
    public void RedactorProvider_Returns_Same_Redactor_For_Logically_Same_Data_Classification()
    {
        var dc1 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification"));
        var dc2 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification2"));
        var dc3 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification3"));
        var dc4 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification4"));
        var dc5 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification5"));
        var dc6 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification6"));
        var dc7 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification7"));
        var dc8 = new DataClassificationSet(new DataClassification("DummyTaxonomy", "Classification8"));

        var dc9 = new DataClassification("DummyTaxonomy", "Classification9");

        var dc1LogicalCopy = new DataClassificationSet(new[] { new DataClassification("DummyTaxonomy", "Classification") });

        var redactorProvider = new ServiceCollection()
        .AddRedaction(redaction =>
        {
            redaction.SetRedactor<NullRedactor>(dc1);
            redaction.SetRedactor<NullRedactor>(dc2);
            redaction.SetRedactor<NullRedactor>(dc3);
            redaction.SetRedactor<NullRedactor>(dc4);
            redaction.SetRedactor<NullRedactor>(dc5);
            redaction.SetRedactor<NullRedactor>(dc6);
            redaction.SetRedactor<NullRedactor>(dc7);
            redaction.SetRedactor<NullRedactor>(dc8);
        })
        .BuildServiceProvider()
        .GetRequiredService<IRedactorProvider>();

        var r1 = redactorProvider.GetRedactor(dc1);
        var r2 = redactorProvider.GetRedactor(dc1LogicalCopy);
        var r3 = redactorProvider.GetRedactor(dc9);

        Assert.Equal(typeof(NullRedactor), r1.GetType());
        Assert.Equal(typeof(NullRedactor), r2.GetType());
        Assert.Equal(typeof(ErasingRedactor), r3.GetType());
    }

    [Fact]
    public void RedactorProvider_Throws_On_Ctor_When_Options_Come_As_Null()
    {
        Assert.Throws<ArgumentException>(() => new RedactorProvider(
            redactors: new Redactor[] { ErasingRedactor.Instance, new FakePlaintextRedactor() },
            options: Microsoft.Extensions.Options.Options.Create<RedactorProviderOptions>(null!)));
    }

    [Fact]
    public void RedactorProvider_Throws_When_Fallback_Redactor_Not_In_DI()
    {
        Assert.Throws<InvalidOperationException>(() => new RedactorProvider(
            Array.Empty<Redactor>(),
            Microsoft.Extensions.Options.Options.Create(new RedactorProviderOptions())));
    }

    [Fact]
    public void RedactorProvider_Throws_When_ModernFallback_Redactor_Not_In_DI()
    {
        var opt = new RedactorProviderOptions
        {
            FallbackRedactor = typeof(FakePlaintextRedactor),
        };

        Assert.Throws<InvalidOperationException>(() => new RedactorProvider(
            new Redactor[]
            {
                ErasingRedactor.Instance,
                NullRedactor.Instance,
            },
            Microsoft.Extensions.Options.Options.Create(opt)));
    }
}
