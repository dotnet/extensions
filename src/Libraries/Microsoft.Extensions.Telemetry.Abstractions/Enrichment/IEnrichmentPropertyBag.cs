// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Telemetry.Enrichment;

/// <summary>
/// Allows enrichers to report enrichment properties.
/// </summary>
public interface IEnrichmentPropertyBag
{
    /// <summary>
    /// Add a property in form of a key/value pair to the bag of enrichment properties.
    /// </summary>
    /// <param name="key">Enrichment property key.</param>
    /// <param name="value">Enrichment property value.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="key"/> or <paramref name="value"/> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// For log enrichment, <paramref name="value"/> is serialized as per the rules below:
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
    /// For metric enrichment, <paramref name="value"/> is converted to <see cref="string"/> format using <see cref="object.ToString()"/> method.
    /// </remarks>
    void Add(string key, object value);

    /// <summary>
    /// Add a property in form of a key/value pair to the bag of enrichment properties.
    /// </summary>
    /// <param name="key">Enrichment property key.</param>
    /// <param name="value">Enrichment property value.</param>
    /// <exception cref="ArgumentException"><paramref name="key"/> is an empty string.</exception>
    /// <exception cref="ArgumentNullException">
    /// Either <paramref name="key"/> or <paramref name="value"/> is <see langword="null" />.
    /// </exception>
    void Add(string key, string value);

    /// <summary>
    /// Adds a series of properties to the bag of enrichment properties.
    /// </summary>
    /// <param name="properties">The properties to add.</param>
    /// <remarks>Refer to the <see cref="Add(string, object)"/> overload for a description of the serialization semantics.</remarks>
    void Add(ReadOnlySpan<KeyValuePair<string, object>> properties);

    /// <summary>
    /// Adds a series of properties to the bag of enrichment properties.
    /// </summary>
    /// <param name="properties">The properties to add.</param>
    void Add(ReadOnlySpan<KeyValuePair<string, string>> properties);
}
