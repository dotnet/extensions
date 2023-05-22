// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Shared.Diagnostics;

#pragma warning disable CA1716
namespace Microsoft.Shared.Data.Validation;
#pragma warning restore CA1716

/// <summary>
/// Provides exclusive boundary validation for <see cref="long"/> or <see cref="double"/> values.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class ExclusiveRangeAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets the minimum value for the range.
    /// </summary>
    public object Minimum { get; }

    /// <summary>
    /// Gets the maximum value for the range.
    /// </summary>
    public object Maximum { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveRangeAttribute"/> class.
    /// </summary>
    /// <param name="minimum">The minimum value, exclusive.</param>
    /// <param name="maximum">The maximum value, exclusive.</param>
    public ExclusiveRangeAttribute(int minimum, int maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExclusiveRangeAttribute"/> class.
    /// </summary>
    /// <param name="minimum">The minimum value, exclusive.</param>
    /// <param name="maximum">The maximum value, exclusive.</param>
    public ExclusiveRangeAttribute(double minimum, double maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Validates that a given value is in range.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">Additional context for this validation.</param>
    /// <returns>A value indicating success or failure.</returns>
    protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
    {
        var comparableMin = Minimum as IComparable;
        var comparableMax = Maximum as IComparable;

        // Minimun and Maximum are either of type int or double, so there is no need for
        // nullability check here (or later) as both types are IComparable already.
        if (comparableMin!.CompareTo(Maximum) >= 0)
        {
            Throw.InvalidOperationException($"{nameof(ExclusiveRangeAttribute)} requires the minimum to be less than the maximum (see field {validationContext.GetDisplayName()})");
        }

        if (value == null)
        {
            // use the [Required] attribute to force presence
            return ValidationResult.Success!;
        }

        var result = comparableMin!.CompareTo(value) < 0 && comparableMax!.CompareTo(value) > 0;

        if (!result)
        {
            return new ValidationResult($"The field {validationContext.GetDisplayName()} must be > {Minimum} and < {Maximum}.", validationContext.GetMemberName());
        }

        return ValidationResult.Success!;
    }
}
