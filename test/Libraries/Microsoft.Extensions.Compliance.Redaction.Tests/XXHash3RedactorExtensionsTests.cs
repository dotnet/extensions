// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class XXHash3RedactorExtensionsTests
{
    [Fact]
    public void DelegateBased()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeRedaction = false)
            .ConfigureRedaction((_, redaction) => redaction
                .SetXXHash3Redactor(o => o.HashSeed = 101, SimpleClassifications.PrivateData))
            .Build();

        var redactorProvider = host.Services.GetRequiredService<IRedactorProvider>();

        CheckProvider(redactorProvider);
    }

    [Fact]
    public void HostBuilder_GivenXXHashRedactorWithConfigurationSectionConfig_RegistersItAsHashingRedactorAndRedacts()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeRedaction = false)
            .ConfigureRedaction((_, redaction) =>
            {
                var section = GetRedactorConfiguration(new ConfigurationBuilder(), 101);
                redaction.SetXXHash3Redactor(section, SimpleClassifications.PrivateData);
            })
            .Build();

        var redactorProvider = host.Services.GetRequiredService<IRedactorProvider>();
        CheckProvider(redactorProvider);
    }

    private static void CheckProvider(IRedactorProvider redactorProvider)
    {
        const string Example = "Redact Me";

        var classifications = new[]
        {
            SimpleClassifications.PublicData,
            SimpleClassifications.PrivateData
        };

        foreach (var dc in classifications)
        {
            var redactor = redactorProvider.GetRedactor(dc);

            var expectedLength = redactor.GetRedactedLength(Example);
            var destination = new char[expectedLength];
            var actualLength = redactor.Redact(Example, destination);

            if (dc == SimpleClassifications.PrivateData)
            {
                Assert.Equal(XXHash3Redactor.RedactedSize, expectedLength);
                Assert.Equal(XXHash3Redactor.RedactedSize, actualLength);
            }
            else
            {
                Assert.True(expectedLength == 0 || expectedLength == Example.Length);
                Assert.True(actualLength == 0 || actualLength == Example.Length);
            }
        }
    }

    private static IConfigurationSection GetRedactorConfiguration(IConfigurationBuilder builder, ulong hashSeed)
    {
        XXHash3RedactorOptions options;

        return builder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(XXHash3RedactorOptions)}:{nameof(options.HashSeed)}", hashSeed.ToString(CultureInfo.InvariantCulture) },
            })
            .Build()
            .GetSection(nameof(XXHash3RedactorOptions));
    }
}
