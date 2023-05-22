// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Options.Validation.Test;

public class OptionsBuilderExtensionsTests
{
    [Fact]
    public void AddValidatedOptions_Throws_WhenNullServices()
    {
        Assert.Throws<ArgumentNullException>(() => OptionsBuilderExtensions.AddValidatedOptions<ComplexOptions>(null!));
    }

    [Theory]
    [InlineData(1, 0, false, true)]
    [InlineData(0, 1, true, false)]
    [InlineData(2, 2, false, false)]
    [InlineData(0, 0, true, true)]
    public async Task OptionsBuilderExtensions_HandlesNamedOptionsProperly(int namedValue, int unnamedValue, bool namedFails, bool unnamedFails)
    {
        const string OptionsName = "Named options";

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddLogging()
                .AddValidatedOptions<NestedOptions, FailingNestedOptionsValidator>(OptionsName)
                .Configure(c => c.Integer = namedValue)
                .Services
                .AddValidatedOptions<NestedOptions, FailingNestedOptionsValidator>()
                .Configure(c => c.Integer = unnamedValue))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAndStopAsync());

        if (!namedFails && !unnamedFails)
        {
            Assert.Null(ex);
            return;
        }

        if (namedFails && unnamedFails)
        {
            Assert.IsType<AggregateException>(ex);
            Assert.Equal(2, ((AggregateException)ex).InnerExceptions.Count);
        }
        else
        {
            if (namedFails)
            {
                Assert.IsType<OptionsValidationException>(ex);
                Assert.Single(((OptionsValidationException)ex).Failures);
                Assert.EndsWith(OptionsName, ((OptionsValidationException)ex).Failures.First());
                Assert.Equal(OptionsName, ((OptionsValidationException)ex).OptionsName);
            }
            else
            {
                Assert.IsType<OptionsValidationException>(ex);
                Assert.Single(((OptionsValidationException)ex).Failures);
                Assert.EndsWith(Microsoft.Extensions.Options.Options.DefaultName, ((OptionsValidationException)ex).Failures.First());
                Assert.Equal(Microsoft.Extensions.Options.Options.DefaultName, ((OptionsValidationException)ex).OptionsName);
            }
        }
    }
}
