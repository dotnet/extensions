// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Extensions for working with the simple data classification taxonomy.
/// </summary>
public static class SimpleTaxonomyExtensions
{
    /// <summary>
    /// Gets the taxonomy value associated with a particular data classification.
    /// </summary>
    /// <param name="classification">The data classification of interest.</param>
    /// <returns>The resulting taxonomy value for the given data classification.</returns>
    public static SimpleTaxonomy AsSimpleTaxonomy(this DataClassification classification)
    {
        if (classification.TaxonomyName != SimpleClassifications.TaxonomyName && !string.IsNullOrEmpty(classification.TaxonomyName))
        {
            Throw.ArgumentException(nameof(classification), $"Unknown data taxonomy: {classification.TaxonomyName}");
        }

        return (SimpleTaxonomy)classification.Value;
    }
}
