// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// Additional state to use with <see cref="M:Microsoft.Extensions.Logging.ILogger.Log``1(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,``0,System.Exception,System.Func{``0,System.Exception,System.String})" />.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class LoggerMessageState : IEnrichmentTagCollector, IReadOnlyList<KeyValuePair<string, object?>>, IEnumerable<KeyValuePair<string, object?>>, IEnumerable, IReadOnlyCollection<KeyValuePair<string, object?>>, ITagCollector
{
    /// <summary>
    /// Represents a captured tag that needs redaction.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ClassifiedTag
    {
        /// <summary>
        /// Gets the name of the tag.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the tag's value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the tag's data classification.
        /// </summary>
        public DataClassification Classification { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Telemetry.Logging.LoggerMessageState.ClassifiedTag" /> struct.
        /// </summary>
        public ClassifiedTag(string name, object? value, DataClassification classification);
    }

    /// <summary>
    /// Gets the array of tags.
    /// </summary>
    public KeyValuePair<string, object?>[] TagArray { get; }

    /// <summary>
    /// Gets the array of tags.
    /// </summary>
    public KeyValuePair<string, object?>[] RedactedTagArray { get; }

    /// <summary>
    /// Gets the array of classified tags.
    /// </summary>
    public ClassifiedTag[] ClassifiedTagArray { get; }

    /// <summary>
    /// Gets a value indicating the number of unclassified tags currently in this instance.
    /// </summary>
    public int NumTags { get; }

    /// <summary>
    /// Gets a value indicating the number of classified tags currently in this instance.
    /// </summary>
    public int NumClassifiedTags { get; }

    /// <inheritdoc />
    public KeyValuePair<string, object?> this[int index] { get; }

    /// <summary>
    /// Gets or sets the parameter name that is prepended to all tag names added to this instance using the
    /// <see cref="M:Microsoft.Extensions.Telemetry.Logging.ITagCollector.Add(System.String,System.Object)" /> or <see cref="M:Microsoft.Extensions.Telemetry.Logging.ITagCollector.Add(System.String,System.Object,Microsoft.Extensions.Compliance.Classification.DataClassification)" />
    /// methods.
    /// </summary>
    public string TagNamePrefix { get; set; }

    /// <summary>
    /// Allocates some room to put some tags.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="P:Microsoft.Extensions.Telemetry.Logging.LoggerMessageState.TagArray" /> where to store the tags.</returns>
    public int ReserveTagSpace(int count);

    /// <summary>
    /// Allocates some room to put some tags.
    /// </summary>
    /// <param name="count">The amount of space to allocate.</param>
    /// <returns>The index in the <see cref="P:Microsoft.Extensions.Telemetry.Logging.LoggerMessageState.ClassifiedTagArray" /> where to store the classified tags.</returns>
    public int ReserveClassifiedTagSpace(int count);

    /// <summary>
    /// Adds a tag to the array.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value.</param>
    public void AddTag(string name, object? value);

    /// <summary>
    /// Adds a classified tag to the array.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The value.</param>
    /// <param name="classification">The data classification of the tag.</param>
    public void AddClassifiedTag(string name, object? value, DataClassification classification);

    /// <summary>
    /// Resets state of this object to its initial condition.
    /// </summary>
    public void Clear();

    /// <summary>
    /// Returns a string representation of this object.
    /// </summary>
    /// <returns>The string representation of this object.</returns>
    public override string ToString();

    public LoggerMessageState();
}
