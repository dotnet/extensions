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

public class AcceptanceTest
{
    [Fact]
    public async Task CanValidateOptionsEagerlyWithDefaultError()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error);
    }

    [Fact]
    public async Task CanValidateOptionsEagerlyWithDefaultError_WithName()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>("bad_configuration")
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error);
    }

    [Fact]
    public async Task CanValidateOptionsEagerlyWithDefaultError_WithNullName()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>(null!)
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error);
    }

    [Fact]
    public async Task CanValidateOptionsEagerThanLazySameType()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean, "first Boolean must be true.")
                .Services
                .AddOptions<ComplexOptions>()
                .Configure(o => o.Boolean = true)
                .Validate(o => !o.Boolean, "second Boolean must be false."))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 1, "second Boolean must be false.");
    }

    [Fact]
    public async Task CanValidateOptionsLazyThanEagerSameType()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                 .AddOptions<ComplexOptions>()
                 .Configure(o => o.Boolean = false)
                 .Validate(o => o.Boolean, "first Boolean must be true.")
                 .Services
                 .AddValidatedOptions<ComplexOptions>()
                 .Configure(o => o.Boolean = true)
                 .Validate(o => !o.Boolean, "second Boolean must be false."))
             .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 1, "second Boolean must be false.");
    }

    [Fact]
    public async Task CanValidateOptionsLazyThanEagerDifferentTypes()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOptions<NestedOptions>()
                .Configure(o => o.Integer = 11)
                .Validate(o => o.Integer > 12, "Integer")
                .Services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean, "first Boolean must be true."))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 1, "first Boolean must be true.");
    }

    [Fact]
    public async Task CanValidateMultipleOptionsSameEagerly()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
               .AddValidatedOptions<ComplexOptions>()
               .Configure(o => o.Integer = 11)
               .Validate(o => o.Integer > 12, "Integer")
               .Services
               .AddValidatedOptions<ComplexOptions>()
               .Configure(o => o.Boolean = false)
               .Validate(o => o.Boolean, "first Boolean must be true."))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 2, "Integer", "first Boolean must be true.");
    }

    [Fact(Skip = "Flaky, see https://github.com/dotnet/r9/issues/171")]
    public async Task CanValidateMultipleOptionsEagerly()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<NestedOptions>()
                .Configure(o => o.Integer = 11)
                .Validate(o => o.Integer > 12, "Integer")
                .Services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean, "first Boolean must be true."))
            .Build();

        var error = await Assert.ThrowsAsync<AggregateException>(() => host.StartAndStopAsync());

        Assert.Equal(2, error.InnerExceptions.Count);

        var errors = error.InnerExceptions;

        // order is not guaranteed, so deal with both possible orderings
        if (errors[0].ToString().Contains("Integer"))
        {
            ValidateFailure<NestedOptions>((OptionsValidationException)errors[0], 1, "Integer");
            ValidateFailure<ComplexOptions>((OptionsValidationException)errors[1], 1, "first Boolean must be true.");
        }
        else
        {
            ValidateFailure<ComplexOptions>((OptionsValidationException)errors[0], 1, "first Boolean must be true.");
            ValidateFailure<NestedOptions>((OptionsValidationException)errors[1], 1, "Integer");
        }
    }

    [Fact]
    public async Task CanValidateOptionsEagerThanLazyDifferentTypes()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<NestedOptions>()
                .Configure(o => o.Integer = 11)
                .Validate(o => o.Integer > 12, "Integer")
                .Services
                .AddOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o => o.Boolean, "first Boolean must be true."))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<NestedOptions>(error, 1, "Integer");
    }

    [Fact]
    public async Task CanValidateOptionsEagerlyWithMultipleDefaultErrors()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Integer > 12))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 2);
    }

    [Fact]
    public async Task CanValidateOptionEagerlysWithMixedOverloads()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                    o.Virtual = "wut";
                })
                .Validate(o => o.Boolean)
                .Validate(o => o.Virtual == null, "Virtual")
                .Validate(o => o.Integer > 12, "Integer"))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 3, "Virtual", "Integer");
    }

    [Fact]
    public async Task CanValidateEagerlyDataAnnotations()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<AnnotatedOptions>()
                .Configure(o =>
                {
                    o.StringLength = "111111";
                    o.IntRange = 10;
                    o.Custom = "nowhere";
                    o.Dep1 = "Not dep2";
                })
                .ValidateDataAnnotations())
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        var optionsName = "'" + nameof(AnnotatedOptions) + "' ";
        ValidateFailure<AnnotatedOptions>(error, 5,
            $"DataAnnotation validation failed for {optionsName}members: 'Required' with the error: 'The Required field is required.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'StringLength' with the error: 'Too long.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'IntRange' with the error: 'Out of range.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'Custom' with the error: 'The field Custom is invalid.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.");
    }

    [Fact]
    public async Task CanValidateEagerlyMixDataAnnotations()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<AnnotatedOptions>()
                .Configure(
                    o =>
                    {
                        o.StringLength = "111111";
                        o.IntRange = 10;
                        o.Custom = "nowhere";
                        o.Dep1 = "Not dep2";
                    })
                .ValidateDataAnnotations()
                .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!"))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        var optionsName = "'" + nameof(AnnotatedOptions) + "' ";
        ValidateFailure<AnnotatedOptions>(error, 6,
            $"DataAnnotation validation failed for {optionsName}members: 'Required' with the error: 'The Required field is required.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'StringLength' with the error: 'Too long.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'IntRange' with the error: 'Out of range.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'Custom' with the error: 'The field Custom is invalid.'.",
            $"DataAnnotation validation failed for {optionsName}members: 'Dep1,Dep2' with the error: 'Dep1 != Dep2'.",
            "I don't want to go to nowhere!");
    }

    [Fact]
    public async Task Test_IValidationSuccessful()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<AnnotatedOptions>()
                .Configure(o =>
                {
                    o.Required = "required";
                    o.StringLength = "1111";
                    o.IntRange = 0;
                    o.Custom = "CZ";
                    o.Dep1 = "dep";
                    o.Dep2 = "dep";
                })
                .Validate(o => o.Custom != "nowhere", "I don't want to go to nowhere!"))
            .Build();

        var exception = await Record.ExceptionAsync(() => host.StartAndStopAsync());

        Assert.Null(exception);
    }

    [Fact(Skip = "Flaky, see https://github.com/dotnet/r9/issues/171")]
    public async Task ValidateOnStart_AddOptionsMultipleTimesForSameType_AllGetTriggered()
    {
        bool firstOptionsBuilderTriggered = false;
        bool secondOptionsBuilderTriggered = false;

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>("bad_configuration1")
                .Configure(o => o.Boolean = false)
                .Validate(o =>
                {
                    firstOptionsBuilderTriggered = true;
                    return o.Boolean;
                }, "Boolean1")
                .Services
                .AddValidatedOptions<ComplexOptions>("bad_configuration2")
                .Configure(o =>
                {
                    o.Boolean = false;
                    o.Integer = 11;
                })
                .Validate(o =>
                {
                    secondOptionsBuilderTriggered = true;
                    return o.Boolean;
                }, "Boolean2")
                .Validate(o => o.Integer > 12, "Integer"))
            .Build();

        var error = await Assert.ThrowsAsync<AggregateException>(() => host.StartAndStopAsync());

        // order is not guaranteed, so handle both possible orderings
        if (error.InnerExceptions[0].ToString().Contains("Boolean1"))
        {
            ValidateFailure<ComplexOptions>((error.InnerExceptions[0] as OptionsValidationException)!, 1, "Boolean1");
            ValidateFailure<ComplexOptions>((error.InnerExceptions[1] as OptionsValidationException)!, 2, "Boolean2", "Integer");
        }
        else
        {
            ValidateFailure<ComplexOptions>((error.InnerExceptions[0] as OptionsValidationException)!, 2, "Boolean2", "Integer");
            ValidateFailure<ComplexOptions>((error.InnerExceptions[1] as OptionsValidationException)!, 1, "Boolean1");
        }

        Assert.True(firstOptionsBuilderTriggered);
        Assert.True(secondOptionsBuilderTriggered);
    }

    [Fact]
    public async Task ValidateOnStart_AddEagerValidation_DoesValidationWhenHostStartsWithNoFailure()
    {
        bool validateCalled = false;

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<ComplexOptions>("correct_configuration")
                .Configure(o => o.Boolean = true)
                .Validate(o =>
                {
                    validateCalled = true;
                    return o.Boolean;
                }, "correct_configuration"))
            .Build();

        await host.StartAndStopAsync();

        Assert.True(validateCalled);
    }

    [Fact]
    public async Task ValidateOnStart_AddLazyValidation_SkipsValidationWhenHostStarts()
    {
        bool validateCalled = false;

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
            .AddValidatedOptions<ComplexOptions>("correct_configuration")
            .Configure(o => o.Boolean = true)
            .Validate(o => o.Boolean, "correct_configuration")
            .Services
            .AddOptions<ComplexOptions>("bad_configuration")
            .Configure(o => o.Boolean = false)
            .Validate(o =>
            {
                validateCalled = true;
                return o.Boolean;
            }, "bad_configuration"))
        .Build();

        // For the lazily added "bad_configuration", validation failure does not occur when host starts
        await host.StartAndStopAsync();

        Assert.False(validateCalled);
    }

    [Fact]
    public async Task ValidateOnStart_AddBothLazyAndEagerValidationOnDifferentTypes_ValidatesWhenHostStartsOnlyForEagerValidations()
    {
        bool validateCalledForNested = false;
        bool validateCalledForComplexOptions = false;

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddOptions<NestedOptions>()
                .Configure(o => o.Integer = 11)
                .Validate(o =>
                {
                    validateCalledForNested = true;
                    return o.Integer > 12;
                }, "Integer")
                .Services
                .AddValidatedOptions<ComplexOptions>()
                .Configure(o => o.Boolean = false)
                .Validate(o =>
                {
                    validateCalledForComplexOptions = true;
                    return o.Boolean;
                }, "first Boolean must be true."))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        ValidateFailure<ComplexOptions>(error, 1, "first Boolean must be true.");

        Assert.False(validateCalledForNested);
        Assert.True(validateCalledForComplexOptions);
    }

    [Fact]
    public async Task ValidationMechanism_Provide_Friendly_Message_To_Debug_When_ValidationException_Is_Thrown()
    {
        var optionsName = "SomeName";

        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddValidatedOptions<InceptionComplexModel, InceptionComplexModelValidator>(optionsName)
                .Configure(x =>
                    x.DeeplyComplexVal = new ComplexModel
                    {
                        ComplexVal = new Model
                        {
                            Val = 4
                        }
                    }))
            .Build();

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAndStopAsync());

        var failureReasons = error.Failures.ToArray();

        Assert.Single(failureReasons);
    }

    private static void ValidateFailure<TOptions>(OptionsValidationException e, int count = 1, params string[] errorsToMatch)
    {
        Assert.Equal(typeof(TOptions), e.OptionsType);
        Assert.Equal(count, e.Failures.ToList().Count);

        // Check for the error in any of the failures
        foreach (var error in errorsToMatch)
        {
#if NETCOREAPP3_1_OR_GREATER
            Assert.True(e.Failures.FirstOrDefault(predicate: f => f.Contains(error, StringComparison.Ordinal)) != null, "Did not find: " + error);
#else
            Assert.True(e.Failures.FirstOrDefault(predicate: f => f.IndexOf(error, StringComparison.Ordinal) >= 0) != null, "Did not find: " + error + " " + e.Failures.First());
#endif
        }
    }
}
