// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Xunit;

#if NETCOREAPP3_1_OR_GREATER
// On newer frameworks use .NET's version of options validation exception.
using OptionsValidationException = Microsoft.Extensions.Options.OptionsValidationException;
#else
using Microsoft.Extensions.Options;
#endif

namespace Microsoft.Extensions.Options.Validation.Test;

public class OptionsValidatorExtensionsTest
{
    [Fact]
    public async Task ShouldValidateOnStartSuccess()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 2))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAndStopAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task ShouldValidateOnStartFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 0))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ValidationHostedService_GivenDataAnnotatedOptionsFailure_ThrowsOptionsValidationException()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<AnnotatedOptions>()
                .Configure(o => o.Dep1 = "")
                .ValidateDataAnnotations())
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateOnStartMultipleModelsSuccess()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model2, Model2Validator>()
                .Configure(o =>
                {
                    o.Val1 = 2;
                    o.Val2 = "ab";
                })
                .Services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 2))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAndStopAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ShouldValidateOnStartOneOfModelsFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model2, Model2Validator>()
                .Configure(o =>
                {
                    o.Val1 = 2;
                    o.Val2 = "abcdef";
                })
                .Services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 2))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateOnStartOneOfModelsFailure_WithName()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model2, Model2Validator>("bad_configuration")
                .Configure(o =>
                {
                    o.Val1 = 2;
                    o.Val2 = "abcdef";
                })
                .Services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 2))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateOnStartOneOfModelsFailure_WithNullName()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model2, Model2Validator>(null!)
                .Configure(o =>
                {
                    o.Val1 = 2;
                    o.Val2 = "abcdef";
                })
                .Services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 2))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateOnStartAllModelsFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<Model2, Model2Validator>()
                .Configure(o =>
                {
                    o.Val1 = 2;
                    o.Val2 = "abcdef";
                })
                .Services
                .AddValidatedOptions<Model, ModelValidator>()
                .Configure(o => o.Val = 4))
            .Build();

        var aggregateException = await Assert.ThrowsAsync<AggregateException>(() => host.StartAndStopAsync());

        Assert.Equal(2, aggregateException.InnerExceptions.Count);
        Assert.IsAssignableFrom<OptionsValidationException>(aggregateException.InnerExceptions[0]);
        Assert.IsAssignableFrom<OptionsValidationException>(aggregateException.InnerExceptions[1]);
    }

    [Fact]
    public async Task ShouldValidateTransitivelyOnStartSuccessfully()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexModel, ComplexModelValidator>()
                .Configure(o =>
                {
                    o.ComplexVal = new Model
                    {
                        Val = 2
                    };

                    o.ValWithoutOptionsValidator = new ModelWithoutOptionsValidator
                    {
                        Val = 6
                    };

                    o.ValWithoutRecursiveValidation = new Model
                    {
                        Val = -1
                    };
                }))
            .Build();

        var ex = await Record.ExceptionAsync(async () => await host.StartAndStopAsync());
        Assert.Null(ex);
    }

    [Fact]
    public async Task ShouldValidateInnerNullModelRecursivelyOnStartSuccessfully()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexModel, ComplexModelValidator>()
                .Configure(o =>
                {
                    o.ComplexVal = null;
                    o.ValWithoutOptionsValidator = null;
                    o.ValWithoutRecursiveValidation = null;
                }))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAndStopAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ShouldValidateTransitivelyOnStartWithFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexModel, ComplexModelValidator>()
                .Configure(o => o.ComplexVal = new Model
                {
                    Val = 0
                }))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateTransitivelyWithoutOptionsValidatorWithFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexModel, ComplexModelValidator>()
                .Configure(o => o.ValWithoutOptionsValidator = new ModelWithoutOptionsValidator
                {
                    Val = 0
                }))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateDeepRecursionOnStartSuccessfully()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<InceptionComplexModel, InceptionComplexModelValidator>()
                .Configure(o => o.DeeplyComplexVal = new ComplexModel
                {
                    ComplexVal = new Model
                    {
                        Val = 2
                    },

                    ComplexValWithSameType = new Model
                    {
                        Val = 1
                    },

                    ValWithoutOptionsValidator = new ModelWithoutOptionsValidator
                    {
                        Val = 9
                    }
                }))
            .Build();

        var ex = await Record.ExceptionAsync(() => host.StartAndStopAsync());

        Assert.Null(ex);
    }

    [Fact]
    public async Task ShouldValidateDeepRecursionOnStartWithFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<InceptionComplexModel, InceptionComplexModelValidator>()
                .Configure(o => o.DeeplyComplexVal = new ComplexModel
                {
                    ComplexVal = new Model
                    {
                        Val = 0
                    }
                }))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(async () => await host.StartAndStopAsync());
    }

    [Fact]
    public async Task ShouldValidateInnerDeepNullModelRecursionOnStartWithFailure()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<InceptionComplexModel, InceptionComplexModelValidator>()
                .Configure(o => o.DeeplyComplexVal = null))
            .Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());
    }
}
