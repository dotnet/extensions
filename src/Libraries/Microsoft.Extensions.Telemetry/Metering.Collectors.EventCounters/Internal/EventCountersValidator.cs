// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Telemetry.Metering.Internal;

internal sealed class EventCountersValidator : IValidateOptions<EventCountersCollectorOptions>
{
    private const string MemberName = nameof(EventCountersCollectorOptions.Counters);

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(EventCountersCollectorOptions))]
    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Addressed by [DynamicDependency]")]
    public ValidateOptionsResult Validate(string? name, EventCountersCollectorOptions options)
    {
        if (options.Counters is null)
        {
            // Nullness is covered in source-generated validator
            return ValidateOptionsResult.Skip;
        }

        var baseName = string.IsNullOrEmpty(name)
            ? nameof(EventCountersCollectorOptions)
            : name;

        var context = new ValidationContext(options);
        var builder = new ValidateOptionsResultBuilder();
        var requiredAttribute = new RequiredAttribute();
        var lengthAttribute = new Microsoft.Shared.Data.Validation.LengthAttribute(1);
        foreach (var pair in options.Counters)
        {
            if (pair.Value is null)
            {
                context.MemberName = MemberName + "[\"" + pair.Key + "\"]";
                context.DisplayName = baseName + "." + context.MemberName;
                builder.AddResult(requiredAttribute.GetValidationResult(pair.Value, context));
            }
            else if (pair.Value.Count == 0)
            {
                context.MemberName = MemberName + "[\"" + pair.Key + "\"]";
                context.DisplayName = baseName + "." + context.MemberName;
                builder.AddResult(lengthAttribute.GetValidationResult(pair.Value, context));
            }
        }

        return builder.Build();
    }
}
