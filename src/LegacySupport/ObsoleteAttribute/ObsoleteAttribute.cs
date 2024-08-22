// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET5_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;

namespace System;

/// <summary>
/// Marks program elements that are no longer in use.
/// </summary>
/// <remarks>
/// Source code imported from 
/// <see href="https://github.com/dotnet/runtime/blob/5535e31a712343a63f5d7d796cd874e563e5ac14/src/libraries/System.Private.CoreLib/src/System/ObsoleteAttribute.cs">
/// ObsoleteAttribute.cs</see> without any changes, all resulting warnings ignored accordingly.
/// </remarks>
#pragma warning disable CA1019 // Define accessors for attribute arguments
#pragma warning disable S3996 // URI properties should not be strings
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
    AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method |
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event |
    AttributeTargets.Delegate,
    Inherited = false)]
internal sealed class ObsoleteAttribute : Attribute
{
    public ObsoleteAttribute()
    {
    }

    public ObsoleteAttribute(string? message)
    {
        Message = message;
    }

    public ObsoleteAttribute(string? message, bool error)
    {
        Message = message;
        IsError = error;
    }

    public string? Message { get; }

    public bool IsError { get; }

    public string? DiagnosticId { get; set; }

    public string? UrlFormat { get; set; }
}

#endif
