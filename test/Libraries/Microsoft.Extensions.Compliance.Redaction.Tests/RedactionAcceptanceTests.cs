// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public class RedactionAcceptanceTests
{
    [Fact]
    public void RedactorProvider_Allows_To_Register_And_Use_Redactors_Using_DataClassification()
    {
        var dc1 = new DataClassification("TAX", "1");
        var dc2 = new DataClassification("TAX", "2");
        var data = "Mississippi";

        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(redaction => redaction
                .SetRedactor<FakePlaintextRedactor>(dc1)
                .SetRedactor<FakePlaintextRedactor>(DataClassification.Unknown)
                .SetFallbackRedactor<FakePlaintextRedactor>())
            .BuildServiceProvider();

        var redactorProvider = services.GetRequiredService<IRedactorProvider>();

        var redactor = redactorProvider.GetRedactor(dc1);
        var redacted = redactor.Redact(data);
        Assert.Equal(redacted, data);

        redactor = redactorProvider.GetRedactor(dc2);
        redacted = redactor.Redact(data);
        Assert.Equal(redacted, data);
    }

    [Fact]
    public void Redaction_Can_Be_Registered_In_Service_Collection()
    {
        var dc = new DataClassification("TAX", "1");
        using var services = new ServiceCollection()
            .AddLogging()
            .AddRedaction(x => x.SetRedactor<FakePlaintextRedactor>(dc))
            .BuildServiceProvider();

        var redactorProvider = services.GetService<IRedactorProvider>();

        Assert.IsAssignableFrom<IRedactorProvider>(redactorProvider);
    }

    [Fact]
    public void Redaction_Extensions_Throws_When_Gets_Null_Args()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRedaction(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddRedaction(null!));
    }

    [Fact]
    public void Can_Register_Redactor_Provider_With_Defaults_Without_Specifying_Arguments()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddRedaction()
            .BuildServiceProvider();

        var dc = new DataClassification("TAX", "1");
        var redactorProvider = serviceProvider.GetRequiredService<IRedactorProvider>();
        var redactor = redactorProvider.GetRedactor(dc);

        Assert.IsAssignableFrom<Redactor>(redactor);
        Assert.IsAssignableFrom<IRedactorProvider>(redactorProvider);
    }
}
