// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Compliance.Classification;

/// <summary>
/// Indicates data which is specifically not classified.
/// </summary>
public sealed class NoDataClassificationAttribute : DataClassificationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoDataClassificationAttribute"/> class.
    /// </summary>
    public NoDataClassificationAttribute()
        : base(DataClassification.None)
    {
    }
}
