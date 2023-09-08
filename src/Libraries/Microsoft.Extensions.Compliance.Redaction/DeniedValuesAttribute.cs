// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET8_0_OR_GREATER

// Stolen from dotnet/runtime/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/DeniedValueAttribute.cs

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter,
        AllowMultiple = false)]
    [ExcludeFromCodeCoverage]
    internal sealed class DeniedValuesAttribute : ValidationAttribute
    {
        public DeniedValuesAttribute(params object?[] values)
        {
            Values = values;
        }

        public object?[] Values { get; }

        public override bool IsValid(object? value)
        {
            foreach (object? denied in Values)
            {
                if (denied is null ? value is null : denied.Equals(value))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#endif
