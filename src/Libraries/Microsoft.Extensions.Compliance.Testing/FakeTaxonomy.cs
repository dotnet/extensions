// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Classes of data used for simple scenarios.
/// </summary>
public enum FakeTaxonomy
{
    /// <summary>
    /// Unknown data classification, handle with care.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// No data classification.
    /// </summary>
    None = 0,

    /// <summary>
    /// This is public data.
    /// </summary>
    PublicData = 1,

    /// <summary>
    /// This is private data.
    /// </summary>
    PrivateData = 2,
}
