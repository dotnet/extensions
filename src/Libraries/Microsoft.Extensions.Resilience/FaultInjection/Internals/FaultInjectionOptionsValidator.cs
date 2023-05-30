// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Resilience.FaultInjection;

internal sealed class FaultInjectionOptionsValidator : IValidateOptions<FaultInjectionOptions>
{
    // This is 100% code coverage. It's likely due to a bug in code coverage that line 19 is marked as not fully covered.
    // Microsoft.Extensions.Resilience.FaultInjection.Test.Options.OptionsValidationTests includes tests to cover this method.
    [ExcludeFromCodeCoverage]
    public static IEnumerable<string> GenerateFailureMessages(ICollection<ValidationResult> validationResults)
    {
        foreach (var result in validationResults)
        {
            yield return $"{result.ErrorMessage ?? "Unknown Error"}";
        }
    }

    public ValidateOptionsResult Validate(string? name, FaultInjectionOptions options)
    {
        foreach (var keyValuePair in options.ChaosPolicyOptionsGroups)
        {
            var optionsGroup = keyValuePair.Value;
            if (optionsGroup.LatencyPolicyOptions != null &&
                !ValidatePolicyOption(optionsGroup.LatencyPolicyOptions, out var latencyOptionsResults))
            {
                throw new OptionsValidationException(name!, typeof(FaultInjectionOptions), GenerateFailureMessages(latencyOptionsResults));
            }

            if (optionsGroup.HttpResponseInjectionPolicyOptions != null &&
                !ValidatePolicyOption(optionsGroup.HttpResponseInjectionPolicyOptions, out var httpOptionsResults))
            {
                throw new OptionsValidationException(name!, typeof(FaultInjectionOptions), GenerateFailureMessages(httpOptionsResults));
            }

            if (optionsGroup.ExceptionPolicyOptions != null && !ValidatePolicyOption(optionsGroup.ExceptionPolicyOptions, out var exceptionOptionsResults))
            {
                throw new OptionsValidationException(name!, typeof(FaultInjectionOptions), GenerateFailureMessages(exceptionOptionsResults));
            }

            if (optionsGroup.CustomResultPolicyOptions != null && !ValidatePolicyOption(optionsGroup.CustomResultPolicyOptions, out var customResultOptionsResults))
            {
                throw new OptionsValidationException(name!, typeof(FaultInjectionOptions), GenerateFailureMessages(customResultOptionsResults));
            }
        }

        return ValidateOptionsResult.Success;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed with [DynamicallyAddressedMembers]")]
    private static bool ValidatePolicyOption<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T obj, out ICollection<ValidationResult> validationResults)
        where T : notnull
    {
        validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(obj, null, null);
        return Validator.TryValidateObject(obj, validationContext, validationResults, true);
    }
}
