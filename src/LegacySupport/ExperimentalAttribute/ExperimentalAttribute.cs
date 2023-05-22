// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// Indicates that an API element is experimental and subject to change without notice.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Struct |
    AttributeTargets.Enum |
    AttributeTargets.Interface |
    AttributeTargets.Delegate |
    AttributeTargets.Method |
    AttributeTargets.Constructor |
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Event |
    AttributeTargets.Assembly)]
internal sealed class ExperimentalAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalAttribute"/> class.
    /// </summary>
    public ExperimentalAttribute()
    {
        // Intentionally left empty.
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentalAttribute"/> class.
    /// </summary>
    /// <param name="message">Human readable explanation for marking experimental API.</param>
    public ExperimentalAttribute(string message)
    {
#pragma warning disable R9A014 // Use the 'Microsoft.Extensions.Diagnostics.Throws' class instead of explicitly throwing exception for improved performance
#pragma warning disable R9A039 // Remove superfluous null check when compiling in a nullable context
#pragma warning disable R9A060 // Consider removing unnecessary null coalescing (??) since the left-hand value is statically known not to be null
#pragma warning disable SA1101 // Prefix local calls with this
        Message = message ?? throw new ArgumentNullException(nameof(message));
#pragma warning restore SA1101 // Prefix local calls with this
#pragma warning restore R9A060 // Consider removing unnecessary null coalescing (??) since the left-hand value is statically known not to be null
#pragma warning restore R9A039 // Remove superfluous null check when compiling in a nullable context
#pragma warning restore R9A014 // Use the 'Microsoft.Extensions.Diagnostics.Throws' class instead of explicitly throwing exception for improved performance
    }

    /// <summary>
    /// Gets a human readable explanation for marking API as experimental.
    /// </summary>
    public string? Message { get; }
}
