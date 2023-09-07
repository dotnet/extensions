// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

namespace Microsoft.Extensions.Telemetry.Enrichment;

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
    /// <exception cref="T:System.ArgumentException"><paramref name="tagName" /> is an empty string.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// Either <paramref name="tagName" /> or <paramref name="tagValue" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// For log enrichment, <paramref name="tagValue" /> is serialized as per the rules below:
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
    ///     <term><see cref="T:System.Collections.IDictionary" /></term>
    ///     <description>recognized as IDictionary&lt;string, object&gt; and serialized in a loop.</description>
    ///  </item>
    ///  <item>
    ///     <term><see cref="T:System.DateTime" /></term>
    ///     <description>recognized and serialized after converting to <see cref="M:System.DateTime.ToUniversalTime" />.</description>
    ///  </item>
    ///  <item>
    ///     <term>All the rest</term>
    ///     <description>converted to <see cref="T:System.String" /> as is and serialized.</description>
    ///  </item>
    /// </list>
    /// For metric enrichment, <paramref name="tagValue" /> is converted to <see cref="T:System.String" /> format using <see cref="M:System.Object.ToString" /> method.
    /// </remarks>
    void Add(string tagName, object tagValue);
}
