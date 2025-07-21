// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>Describes the portion of an associated <see cref="AIContent"/> to which an annotation applies.</summary>
/// <remarks>
/// Details about the region is provided by derived types, such as <see cref="TextSpanAnnotatedRegion"/>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSpanAnnotatedRegion), typeDiscriminator: "textSpan")]
public class AnnotatedRegion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnnotatedRegion"/> class.
    /// </summary>
    public AnnotatedRegion()
    {
    }
}
