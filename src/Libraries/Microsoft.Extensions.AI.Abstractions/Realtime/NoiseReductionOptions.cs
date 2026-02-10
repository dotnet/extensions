// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring a real-time session.
/// </summary>
[Experimental("MEAI001")]
public enum NoiseReductionOptions
{
    /// <summary>
    /// No noise reduction applied.
    /// </summary>
    None,

    /// <summary>
    /// for close-talking microphones.
    /// </summary>
    NearField,

    /// <summary>
    /// For far-field microphones.
    /// </summary>
    FarField
}
