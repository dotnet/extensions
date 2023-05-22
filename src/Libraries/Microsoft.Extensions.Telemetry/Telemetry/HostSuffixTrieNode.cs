// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Telemetry;
internal sealed class HostSuffixTrieNode
{
    private const int ASCIICharCount = 128;

    public string DependencyName { get; set; } = string.Empty;

    public HostSuffixTrieNode[] Nodes { get; } = new HostSuffixTrieNode[ASCIICharCount];
}
