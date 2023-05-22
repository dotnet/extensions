// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Xunit;
using Opt = Microsoft.Extensions.Options.Options;

namespace Microsoft.Extensions.Options.Validation.Test;

public class ValidationHostedServiceTests
{
    [Fact]
    public void ValidationHostedService_Throws_Exception_When_Provided_Options_Are_Invalid()
    {
        var options = Opt.Create(new ValidatorOptions());

        Assert.Throws<ArgumentException>(() => new ValidationHostedService(options));
        Assert.Throws<ArgumentException>(() => new ValidationHostedService(Opt.Create<ValidatorOptions>(null!)));
    }

    [Fact]
    public async Task Validation_Throws_WhenValidatorThrows()
    {
        var options = Opt.Create(new ValidatorOptions());
        options.Value.Validators[(typeof(object), string.Empty)] = () => throw new ValidationException();
        var service = new ValidationHostedService(options);
        var ex = await Assert.ThrowsAsync<OptionsValidationException>(() => service.StartAsync(CancellationToken.None));
        Assert.NotEmpty(ex.Failures);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Validation_Throws_AggregateExc_WhenMultipleValidators()
    {
        var options = Opt.Create(new ValidatorOptions());
        options.Value.Validators[(typeof(object), string.Empty)] = () => throw new ValidationException();
        options.Value.Validators[(typeof(string), string.Empty)] = () => throw new ValidationException();
        var service = new ValidationHostedService(options);
        await Assert.ThrowsAsync<AggregateException>(() => service.StartAsync(CancellationToken.None));

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Validation_Throws_WhenTokenIsCancelled()
    {
        var options = Opt.Create(new ValidatorOptions());
        options.Value.Validators[(typeof(object), string.Empty)] = () => throw new ValidationException();
        options.Value.Validators[(typeof(string), string.Empty)] = () => throw new ValidationException();
        var service = new ValidationHostedService(options);

        using var tokenSource = new CancellationTokenSource();
        tokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.StartAsync(tokenSource.Token));

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ValidatorHostedService_Fills_Invalid_Members_In_Friendly_Message()
    {
        const string OptionsName = "Jack Sparrow";

        const string FailureMember1 = "Camembert1";
        const string FailureMember2 = "Camembert2";
        const string FailureMember3 = "Camembert3";

        const string ErrorMessage = "I will fail forever :(.";

        var options = Opt.Create(new ValidatorOptions());
        options.Value.Validators[(typeof(object), OptionsName)] = () => throw new ValidationException(
            validationResult: new ValidationResult(ErrorMessage, new[] { FailureMember1, FailureMember2, FailureMember3 }), new RangeAttribute(0, 10), new object());

        var service = new ValidationHostedService(options);

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => service.StartAsync(CancellationToken.None));
        var failureReasons = error.Failures.ToArray();

        Assert.Single(failureReasons);

        Assert.Contains(typeof(object).FullName!, failureReasons[0]);
        Assert.Contains(OptionsName, failureReasons[0]);
        Assert.Contains(ErrorMessage, failureReasons[0]);
        Assert.Contains(FailureMember1, failureReasons[0]);
        Assert.Contains(FailureMember2, failureReasons[0]);
        Assert.Contains(FailureMember3, failureReasons[0]);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ValidatorHostedService_Fills_Failed_Members_To_Be_Unknown_When_Not_Provided()
    {
        const string OptionsName = "Jack Sparrow";

        const string ErrorMessage = "I will fail forever :(.";

        var options = Opt.Create(new ValidatorOptions());
        options.Value.Validators[(typeof(object), OptionsName)] = () => throw new ValidationException(
            validationResult: new ValidationResult(ErrorMessage, Array.Empty<string>()), new RangeAttribute(0, 10), new object());

        var service = new ValidationHostedService(options);

        var error = await Assert.ThrowsAsync<OptionsValidationException>(() => service.StartAsync(CancellationToken.None));
        var failureReasons = error.Failures.ToArray();

        Assert.Single(failureReasons);

        Assert.Contains(typeof(object).FullName!, failureReasons[0]);
        Assert.Contains(OptionsName, failureReasons[0]);
        Assert.Contains(ErrorMessage, failureReasons[0]);
        Assert.Contains(ValidationHostedService.Unknown, failureReasons[0]);

        await service.StopAsync(CancellationToken.None);
    }
}
#endif
