// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S2365 // Properties should not make collection or array copies
#pragma warning disable S3604 // Member initializer values should not be redundant

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

/// <summary>Provides a dictionary used as the AdditionalProperties dictionary on Microsoft.Extensions.AI objects.</summary>
public sealed class AdditionalPropertiesDictionary : AdditionalPropertiesDictionary<object?>
{
    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary()
        : base()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IDictionary<string, object?> dictionary)
        : base(dictionary)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AdditionalPropertiesDictionary"/> class.</summary>
    public AdditionalPropertiesDictionary(IEnumerable<KeyValuePair<string, object?>> collection)
        : base(collection)
    {
    }

    /// <summary>Creates a shallow clone of the properties dictionary.</summary>
    /// <returns>
    /// A shallow clone of the properties dictionary. The instance will not be the same as the current instance,
    /// but it will contain all of the same key-value pairs.
    /// </returns>
    public new AdditionalPropertiesDictionary Clone() => new(this);
}
