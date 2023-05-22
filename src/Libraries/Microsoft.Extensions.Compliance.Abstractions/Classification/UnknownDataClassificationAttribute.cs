// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Indicates data whose classification is unknown.
/// </summary>
public sealed class UnknownDataClassificationAttribute : DataClassificationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownDataClassificationAttribute"/> class.
    /// </summary>
    public UnknownDataClassificationAttribute()
        : base(DataClassification.Unknown)
    {
    }
}
