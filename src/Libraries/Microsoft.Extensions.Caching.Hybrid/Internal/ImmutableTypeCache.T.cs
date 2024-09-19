// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid.Internal;

/// <summary>
/// Simple memoize storage for whether the type <typeparamref name="T"/> is blittable, in particular to avoid repeated runtime tests
/// in down-level TFMs where this is trickier to determine. The JIT is very effective at accessing this memoized value.
/// </summary>
/// <typeparam name="T">The type being processed.</typeparam>
internal static class ImmutableTypeCache<T> // lazy memoize; T doesn't change per cache instance
{
    // note for blittable types: a pure struct will be a full copy every time - nothing shared to mutate
    public static readonly bool IsImmutable = (typeof(T).IsValueType && ImmutableTypeCache.IsBlittable<T>()) || ImmutableTypeCache.IsTypeImmutable(typeof(T));
}
