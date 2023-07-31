// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Telemetry.Logging;

public partial class LoggerMessageState
{
    /// <summary>
    /// Represents a captured property that needs redaction.
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not for customer use and hidden from docs")]
    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ClassifiedProperty
    {
        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public readonly string Name { get; }

        /// <summary>
        /// Gets the property's value.
        /// </summary>
        public readonly object? Value { get; }

        /// <summary>
        /// Gets the property's data classification.
        /// </summary>
        public readonly DataClassification Classification { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassifiedProperty"/> struct.
        /// </summary>
        public ClassifiedProperty(string name, object? value, DataClassification classification)
        {
            Name = name;
            Value = value;
            Classification = classification;
        }
    }
}
