// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Allows enrichers to report enrichment properties.
/// </summary>
public interface IEnrichmentTagCollector
{
    /// <summary>
    /// Adds a tag in form of a key/value pair.
    /// </summary>
    /// <param name="tagName">Enrichment property key.</param>
    /// <param name="tagValue">Enrichment property value.</param>
    /// <exception cref="ArgumentException"><paramref name="tagName"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="tagName"/> or <paramref name="tagValue"/> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// For log enrichment, <paramref name="tagValue"/> is serialized as per the rules below:
    /// <list type="bullet">
    /// <listheader>
    ///    <term>Primitive types</term>
    ///    <description>recognized and efficiently serialized.</description>
    ///  </listheader>
    /// <item>
    ///     <term>Arrays</term>
    ///     <description>recognized and serialized in a loop.</description>
    ///  </item>
    ///  <item>
    ///     <term><see cref="IDictionary"/></term>
    ///     <description>recognized as IDictionary&lt;string, object&gt; and serialized in a loop.</description>
    ///  </item>
    ///  <item>
    ///     <term><see cref="DateTime"/></term>
    ///     <description>recognized and serialized after converting to <see cref="DateTime.ToUniversalTime()"/>.</description>
    ///  </item>
    ///  <item>
    ///     <term>All the rest</term>
    ///     <description>converted to <see cref="string"/> as is and serialized.</description>
    ///  </item>
    /// </list>
    /// For metric enrichment, <paramref name="tagValue"/> is converted to <see cref="string"/> format using <see cref="object.ToString()"/> method.
    /// </remarks>
    void Add(string tagName, object tagValue);
}
