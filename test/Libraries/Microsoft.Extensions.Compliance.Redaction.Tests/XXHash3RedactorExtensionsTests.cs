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

namespace Microsoft.Extensions.Compliance.Redaction.Test;

public class XxHash3RedactorExtensionsTests
{
    [Fact]
    public void DelegateBased()
    {
        using var host = FakeHost.CreateBuilder(options => options.FakeRedaction = false)
            .ConfigureRedaction((_, redaction) => redaction
                .SetXxHash3Redactor(o => o.HashSeed = 101, FakeClassifications.PrivateData))
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
                redaction.SetXxHash3Redactor(section, FakeClassifications.PrivateData);
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
            FakeClassifications.PublicData,
            FakeClassifications.PrivateData
        };

        foreach (var dc in classifications)
        {
            var redactor = redactorProvider.GetRedactor(dc);

            var expectedLength = redactor.GetRedactedLength(Example);
            var destination = new char[expectedLength];
            var actualLength = redactor.Redact(Example, destination);

            if (dc == FakeClassifications.PrivateData)
            {
                Assert.Equal(XxHash3Redactor.RedactedSize, expectedLength);
                Assert.Equal(XxHash3Redactor.RedactedSize, actualLength);
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
        XxHash3RedactorOptions options;

        return builder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(XxHash3RedactorOptions)}:{nameof(options.HashSeed)}", hashSeed.ToString(CultureInfo.InvariantCulture) },
            })
            .Build()
            .GetSection(nameof(XxHash3RedactorOptions));
    }
}
