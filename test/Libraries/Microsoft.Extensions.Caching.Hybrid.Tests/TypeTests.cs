// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Caching.Hybrid.Internal;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class TypeTests
{
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))] // primitive
    [InlineData(typeof(int?))]
    [InlineData(typeof(Guid))] // non-primitive but blittable
    [InlineData(typeof(Guid?))]
    [InlineData(typeof(SealedCustomClassAttribTrue))] // attrib says explicitly true, and sealed
    [InlineData(typeof(CustomBlittableStruct))] // blittable, and we're copying each time
    [InlineData(typeof(CustomNonBlittableStructAttribTrue))] // non-blittable, attrib says explicitly true
    public void ImmutableTypes(Type type)
    {
        Assert.True((bool)typeof(DefaultHybridCache.ImmutableTypeCache<>).MakeGenericType(type)
            .GetField(nameof(DefaultHybridCache.ImmutableTypeCache<string>.IsImmutable), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!);
    }

    [Theory]
    [InlineData(typeof(byte[]))]
    [InlineData(typeof(string[]))]
    [InlineData(typeof(object))]
    [InlineData(typeof(CustomClassNoAttrib))] // no attrib, who knows?
    [InlineData(typeof(CustomClassAttribFalse))] // attrib says explicitly no
    [InlineData(typeof(CustomClassAttribTrue))] // attrib says explicitly true, but not sealed: we might have a sub-class
    [InlineData(typeof(CustomNonBlittableStructNoAttrib))] // no attrib, who knows?
    [InlineData(typeof(CustomNonBlittableStructAttribFalse))] // attrib says explicitly no
    public void MutableTypes(Type type)
    {
        Assert.False((bool)typeof(DefaultHybridCache.ImmutableTypeCache<>).MakeGenericType(type)
            .GetField(nameof(DefaultHybridCache.ImmutableTypeCache<string>.IsImmutable), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!);
    }

    private class CustomClassNoAttrib
    {
    }

    [ImmutableObject(false)]
    private class CustomClassAttribFalse
    {
    }

    [ImmutableObject(true)]
    private class CustomClassAttribTrue
    {
    }

    [ImmutableObject(true)]
    private sealed class SealedCustomClassAttribTrue
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Needed to be non-trivial blittable")]
    private struct CustomBlittableStruct(int x)
    {
        public readonly int X => x;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Needed to force non-blittable")]
    private struct CustomNonBlittableStructNoAttrib(string x)
    {
        public readonly string X => x;
    }

    [ImmutableObject(false)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Needed to force non-blittable")]
    private struct CustomNonBlittableStructAttribFalse(string x)
    {
        public readonly string X => x;
    }

    [ImmutableObject(true)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "Needed to force non-blittable")]
    private struct CustomNonBlittableStructAttribTrue(string x)
    {
        public readonly string X => x;
    }
}
