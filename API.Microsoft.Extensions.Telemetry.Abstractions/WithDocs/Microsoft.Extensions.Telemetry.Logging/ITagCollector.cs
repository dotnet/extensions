// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Interface given to custom tag providers, enabling them to emit tags.
/// </summary>
/// <remarks>
/// See <see cref="T:Microsoft.Extensions.Telemetry.Logging.TagProviderAttribute" /> for details on how this interface is used.
/// </remarks>
public interface ITagCollector
{
    /// <summary>
    /// Adds a tag.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <param name="tagValue">The value of the tag to add.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="tagName" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException"><paramref name="tagName" /> is empty or contains exclusively whitespace,
    /// or when a tag of the same name has already been added.
    /// </exception>
    void Add(string tagName, object? tagValue);

    /// <summary>
    /// Adds a tag.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <param name="tagValue">The value of the tag to add.</param>
    /// <param name="classification">The data classification of the tag value.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="tagName" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException"><paramref name="tagName" /> is empty or contains exclusively whitespace,
    /// or when a tag of the same name has already been added.
    /// </exception>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    void Add(string tagName, object? tagValue, DataClassification classification);
}
