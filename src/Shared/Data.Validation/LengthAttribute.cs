// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#pragma warning disable CA1716
namespace Microsoft.Shared.Data.Validation;
#pragma warning restore CA1716

/// <summary>
/// Specifies the minimum length of any <see cref="IEnumerable"/> or <see cref="string"/> objects.
/// </summary>
/// <remarks>
/// The standard <see cref="MinLengthAttribute" /> supports only non generic <see cref="Array"/> or <see cref="string"/> typed objects
/// on .NET Framework, while <see cref="System.Collections.Generic.ICollection{T}"/> type is supported only on .NET Core.
/// See issue here <see href="https://github.com/dotnet/runtime/issues/23288"/>.
/// This attribute aims to allow validation of all these objects in a consistent manner across target frameworks.
/// </remarks>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]

internal sealed class LengthAttribute : ValidationAttribute
{
    /// <summary>
    /// Gets the minimum allowed length of the collection or string.
    /// </summary>
    public int MinimumLength { get; }

    /// <summary>
    /// Gets the maximum allowed length of the collection or string.
    /// </summary>
    public int? MaximumLength { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the length validation should exclude the <see cref="MinimumLength"/> and <see cref="MaximumLength"/> values.
    /// </summary>
    /// <remarks>
    /// By default the property is set to <c>false</c>.
    /// </remarks>
    public bool Exclusive { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthAttribute"/> class.
    /// </summary>
    /// <param name="minimumLength">
    /// The minimum allowable length of array/string data.
    /// Value must be greater than or equal to zero.
    /// </param>
    [RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
    public LengthAttribute(int minimumLength)
    {
        MinimumLength = minimumLength;
        MaximumLength = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthAttribute"/> class.
    /// </summary>
    /// <param name="minimumLength">
    /// The minimum allowable length of array/string data.
    /// Value must be greater than or equal to zero.
    /// </param>
    /// <param name="maximumLength">
    /// The maximum allowable length of array/string data.
    /// Value must be greater than or equal to zero.
    /// </param>
    [RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
    public LengthAttribute(int minimumLength, int maximumLength)
    {
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
    }

    /// <summary>
    /// Validates that a given value is in range.
    /// </summary>
    /// <remarks>
    /// This method returns <c>true</c> if the <paramref name = "value" /> is null.
    /// It is assumed the <see cref = "RequiredAttribute" /> is used if the value may not be null.
    /// </remarks>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">Additional context for this validation.</param>
    /// <returns>A value indicating success or failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException">if <see cref="MinimumLength"/> is less than zero or if it is greater than <see cref="MaximumLength"/>.</exception>
    /// <exception cref="InvalidOperationException">if the validated type is not supported.</exception>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The ctor is marked with RequiresUnreferencedCode.")]
    protected override ValidationResult IsValid(object? value, ValidationContext? validationContext)
    {
        if (MinimumLength < 0)
        {
            throw new InvalidOperationException($"{nameof(LengthAttribute)} requires a minimum length >= 0 (see field {validationContext.GetDisplayName()})");
        }

        if (MaximumLength.HasValue && MinimumLength >= MaximumLength)
        {
            throw new InvalidOperationException($"{nameof(LengthAttribute)} requires the minimum length to be less than maximum length (see field {validationContext.GetDisplayName()})");
        }

        // Automatically pass if value is null. RequiredAttribute should be used to assert a value is not null.
        if (value == null)
        {
            return ValidationResult.Success!;
        }

        int count;
        switch (value)
        {
            case string s:
                count = s.Length;
                break;

            case ICollection c:
                count = c.Count;
                break;

            case IEnumerable e:
                count = 0;
                foreach (var item in e)
                {
                    count++;
                }

                break;

            default:
                var property = GetCountProperty(value);
                if (property != null && property.CanRead && property.PropertyType == typeof(int))
                {
                    count = (int)property.GetValue(value)!;
                }
                else
                {
                    throw new InvalidOperationException($"{nameof(LengthAttribute)} is not supported for fields of type {value.GetType()} (see field {validationContext.GetDisplayName()})");
                }

                break;
        }

        return Validate(count, validationContext);
    }

    [RequiresUnreferencedCode("Uses reflection to get the 'Count' property on types that don't implement ICollection. This 'Count' property may be trimmed. Ensure it is preserved.")]
    private static PropertyInfo? GetCountProperty(object value) => value.GetType().GetRuntimeProperty("Count");

    private ValidationResult Validate(int count, ValidationContext? validationContext)
    {
        bool result;

        if (MaximumLength.HasValue)
        {
            // Minimum and maximum length validation.
            result = Exclusive
                ? count > MinimumLength && count < MaximumLength
                : count >= MinimumLength && count <= MaximumLength;
        }
        else
        {
            // Minimum length validation only.
            result = Exclusive
                ? count > MinimumLength
                : count >= MinimumLength;
        }

        if (!result)
        {
            if (!string.IsNullOrEmpty(ErrorMessage) || !string.IsNullOrEmpty(ErrorMessageResourceName))
            {
                return new ValidationResult(FormatErrorMessage(validationContext.GetDisplayName()), validationContext.GetMemberName());
            }

            var exclusiveString = Exclusive ? "exclusive " : string.Empty;
            var orEqualString = Exclusive ? string.Empty : "or equal ";
            var validationMessage = MaximumLength.HasValue
                ? $"The field {validationContext.GetDisplayName()} length must be in the {exclusiveString}range [{MinimumLength}..{MaximumLength}]."
                : $"The field {validationContext.GetDisplayName()} length must be greater {orEqualString}than {MinimumLength}.";

            return new ValidationResult(validationMessage, validationContext.GetMemberName());
        }

        return ValidationResult.Success!;
    }
}
