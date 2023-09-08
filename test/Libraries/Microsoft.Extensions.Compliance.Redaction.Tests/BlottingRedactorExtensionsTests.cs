// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Compliance.Redaction.Tests;

public class BlottingRedactorExtensionsTests
{
    [Fact]
    public void DelegateBased()
    {
        var redactorProvider = new ServiceCollection()
            .AddRedaction(redaction => redaction.SetBlottingRedactor(o => o.BlottingCharacter = 'X', SimpleClassifications.PrivateData))
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
                var section = GetRedactorConfiguration(new ConfigurationBuilder(), 'X');
                redaction.SetBlottingRedactor(section, SimpleClassifications.PrivateData);
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
                Assert.Equal(Example.Length, expectedLength);
                Assert.Equal(Example.Length, actualLength);
                Assert.Equal(new string('X', Example.Length), new string(destination));
            }
            else
            {
                Assert.Equal(0, expectedLength);
                Assert.Equal(0, actualLength);
            }
        }
    }

    private static IConfigurationSection GetRedactorConfiguration(IConfigurationBuilder builder, char blottingChar)
    {
        BlottingRedactorOptions options;

        return builder
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { $"{nameof(BlottingRedactorOptions)}:{nameof(options.BlottingCharacter)}", blottingChar.ToString() },
            })
            .Build()
            .GetSection(nameof(BlottingRedactorOptions));
    }
}
