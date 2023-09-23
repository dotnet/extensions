// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Diagnostics;

internal sealed class RequestMetadataTrieNode
{
    public byte ChildNodesCount { get; set; }
    public char YoungestChild { get; set; } = (char)Constants.ASCIICharCount;
    public char EldestChild { get; set; }
    public char Delimiter { get; set; } = Constants.DefaultRouteEndDelim;

    public RequestMetadata? RequestMetadata { get; set; }

    public RequestMetadataTrieNode? Parent { get; set; }

    // The property has actually 100% coverage, but due to a bug in the code coverage tool,
    // a lower number is reported. Therefore, we temporarily exclude this property
    // from the coverage measurements. Once the bug in the code coverage tool is fixed,
    // the exclusion attribute can be removed.
    [ExcludeFromCodeCoverage]
    public RequestMetadataTrieNode[] Nodes { get; } = new RequestMetadataTrieNode[Constants.ASCIICharCount];
}
