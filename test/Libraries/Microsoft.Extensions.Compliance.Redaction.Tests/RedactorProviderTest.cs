// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class RedactorProviderTest
{
    [Fact]
    public void RedactorProvider_Returns_Redactor_For_Every_Data_Classification()
    {
        var dc = new DataClassification("Foo", 0x2);

        var redactorProvider = new RedactorProvider(
            redactors: new Redactor[] { ErasingRedactor.Instance, NullRedactor.Instance },
            options: Microsoft.Extensions.Options.Options.Create(new RedactorProviderOptions()));

        var r = redactorProvider.GetRedactor(dc);
        Assert.IsAssignableFrom<Redactor>(r);
    }

    private static readonly DataClassification _dataClassification1 = new("TAX", 1);
    private static readonly DataClassification _dataClassification2 = new("TAX", 2);
    private static readonly DataClassification _dataClassification3 = new("TAX", 4);

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
