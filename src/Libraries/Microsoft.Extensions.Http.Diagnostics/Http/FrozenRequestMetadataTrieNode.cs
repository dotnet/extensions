// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Diagnostics;

internal sealed class FrozenRequestMetadataTrieNode
{
    public char Delimiter { get; set; } = Constants.DefaultRouteEndDelim;
    public byte ChildNodesCount { get; set; }
    public char YoungestChild { get; set; }
    public int ChildStartIndex { get; set; }
    public int RequestMetadataEntryIndex { get; set; } = -1;
}
