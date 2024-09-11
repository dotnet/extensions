// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Reflection;

#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
using System.Runtime.CompilerServices;
#else
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
#endif

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// Utility type for determining whether a type is blittable; the logic for this is very
/// TFM dependent.
/// </summary>
internal static class ImmutableTypeCache
{
    internal static bool IsBlittable<T>() // minimize the generic portion (twinned with IsTypeImmutable)
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return !RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        // down-level: only blittable types can be pinned
        try
        {
            // get a typed, zeroed, non-null boxed instance of the appropriate type
            // (can't use (object)default(T), as that would box to null for nullable types)
            var obj = FormatterServices.GetUninitializedObject(Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T));
            GCHandle.Alloc(obj, GCHandleType.Pinned).Free();
            return true;
        }
#pragma warning disable CA1031 // Do not catch general exception types: interpret any failure here as "nope"
        catch
        {
            return false;
        }
#pragma warning restore CA1031

#endif
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Blocker Code Smell", "S2178:Short-circuit logic should be used in boolean contexts",
        Justification = "Non-short-circuiting intentional to remove unnecessary branch")]
    internal static bool IsTypeImmutable(Type type)
    {
        // check for known types
        if (type == typeof(string))
        {
            return true;
        }

        if (type.IsValueType)
        {
            // switch from Foo? to Foo if necessary
            if (Nullable.GetUnderlyingType(type) is { } nullable)
            {
                type = nullable;
            }
        }

        if (type.IsValueType || (type.IsClass & type.IsSealed))
        {
            // check for [ImmutableObject(true)]; note we're looking at this as a statement about
            // the overall nullability; for example, a type could contain a private int[] field,
            // where the field is mutable and the list is mutable; but if the type is annotated:
            // we're trusting that the API and use-case is such that the type is immutable
            return type.GetCustomAttribute<ImmutableObjectAttribute>() is { Immutable: true };
        }

        // don't trust interfaces and non-sealed types; we might have any concrete
        // type that has different behaviour
        return false;
    }
}
