// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Hedging;

/// <summary>
/// A null struct for policies and actions which do not return a TResult.
/// </summary>
internal readonly struct EmptyStruct
{
    /// <summary>
    /// Initializes a new instance of the EmptyStruct for policies which do not return a result." /> structure.
    /// </summary>
    public static readonly EmptyStruct Instance;
}
