// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class HmacRedactorExtensionsTests
{
    [Fact]
    public void DelegateBased()
    {
        var redactorProvider = new ServiceCollection()
            .AddRedaction(redaction => redaction.SetXXHash3Redactor(o => o.HashSeed = 101, SimpleClassifications.PrivateData))
            .BuildServiceProvider()
            .GetRequiredService<IRedactorProvider>();

        CheckProvider(redactorProvider);
    }

    [Fact]
    public void GivenRedactorWithConfigurationSectionConfig_RegistersItAsHashingRedactorAndRedacts()
    {
        var redactorProvider = new ServiceCollection()
            .AddRedaction(redaction =>
            {
                var section = HmacRedactorTest.GetRedactorConfiguration(new ConfigurationBuilder(), HmacRedactorTest.HmacExamples[0].KeyId, HmacRedactorTest.HmacExamples[0].Key);
                redaction.SetHmacRedactor(section, SimpleClassifications.PrivateData);
            })
            .BuildServiceProvider()
            .GetRequiredService<IRedactorProvider>();

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
                Assert.Equal(expectedLength, actualLength);
            }
            else
            {
                Assert.True(expectedLength == 0 || expectedLength == Example.Length);
                Assert.True(actualLength == 0 || actualLength == Example.Length);
            }
        }
    }
}
