// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Classes of data used for simple scenarios.
/// </summary>
public enum FakeTaxonomy : ulong
{
    /// <summary>
    /// No data classification.
    /// </summary>
    None,

    /// <summary>
    /// This is public data.
    /// </summary>
    PublicData,

    /// <summary>
    /// This is private data.
    /// </summary>
    PrivateData,
}
