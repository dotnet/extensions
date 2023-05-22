// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry.Internal;

namespace Microsoft.Extensions.Telemetry;

internal sealed class DownstreamDependencyMetadataManager : IDownstreamDependencyMetadataManager
{
    internal readonly struct ProcessedMetadata
    {
        public FrozenRequestMetadataTrieNode[] Nodes { get; init; }
        public RequestMetadata[] RequestMetadatas { get; init; }
    }

    private const char AsteriskChar = '*';
    private static readonly Regex _routeRegex = DownstreamDependencyMetadataManagerRegex.MakeRouteRegex();
    private static readonly char[] _toUpper = MakeToUpperArray();

    private readonly HostSuffixTrieNode _hostSuffixTrieRoot = new();
    private readonly FrozenDictionary<string, ProcessedMetadata> _frozenProcessedMetadataMap;

    public DownstreamDependencyMetadataManager(IEnumerable<IDownstreamDependencyMetadata> downstreamDependencyMetadata)
    {
        Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap = new();
        foreach (var dependency in downstreamDependencyMetadata)
        {
            AddDependency(dependency, dependencyTrieMap);
        }

        _frozenProcessedMetadataMap = ProcessDownstreamDependencyMetadata(dependencyTrieMap).ToFrozenDictionary(StringComparer.Ordinal, optimizeForReading: true);
    }

    public RequestMetadata? GetRequestMetadata(HttpRequestMessage requestMessage)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            if (requestMessage.RequestUri == null)
            {
                return null;
            }

            string dependencyName = GetHostDependencyName(requestMessage.RequestUri.Host);
            return GetRequestMetadataInternal(requestMessage.Method.Method, requestMessage.RequestUri.AbsolutePath, dependencyName);
        }
        catch (Exception)
        {
            // Catch exceptions here to avoid impacting services if a bug ever gets introduced in this path.
            return null;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    public RequestMetadata? GetRequestMetadata(HttpWebRequest requestMessage)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            string dependencyName = GetHostDependencyName(requestMessage.RequestUri.Host);
            return GetRequestMetadataInternal(requestMessage.Method, requestMessage.RequestUri.AbsolutePath, dependencyName);
        }
        catch (Exception)
        {
            // Catch exceptions here to avoid impacting services if a bug ever gets introduced in this path.
            return null;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static char[] MakeToUpperArray()
    {
        // Initialize the _toUpper array for quick conversion of any ascii char to upper
        // without incurring cost of checking whether the character requires conversion.

        var a = new char[Constants.ASCIICharCount];
        for (int i = 0; i < Constants.ASCIICharCount; i++)
        {
            a[i] = char.ToUpperInvariant((char)i);
        }

        return a;
    }

    private static void AddRouteToTrie(RequestMetadata routeMetadata, Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap)
    {
        if (!dependencyTrieMap.TryGetValue(routeMetadata.DependencyName, out var routeMetadataTrieRoot))
        {
            routeMetadataTrieRoot = new RequestMetadataTrieNode();
            dependencyTrieMap.Add(routeMetadata.DependencyName, routeMetadataTrieRoot);
        }

        var trieCurrent = routeMetadataTrieRoot;
        trieCurrent.Parent = trieCurrent;

        if (routeMetadata.RequestRoute[0] != '/')
        {
            routeMetadata.RequestRoute = $"/{routeMetadata.RequestRoute}";
        }

        var route = _routeRegex.Replace(routeMetadata.RequestRoute, "*");
        route = route.ToUpperInvariant();
        for (int i = 0; i < route.Length; i++)
        {
            char ch = route[i];
            if (ch >= Constants.ASCIICharCount)
            {
                return;
            }

            trieCurrent.Nodes[ch] ??= new();
            trieCurrent.YoungestChild = ch < trieCurrent.YoungestChild ? ch : trieCurrent.YoungestChild;
            trieCurrent.EldestChild = ch > trieCurrent.EldestChild ? ch : trieCurrent.EldestChild;
            trieCurrent.ChildNodesCount = (byte)(trieCurrent.EldestChild - trieCurrent.YoungestChild + 1);
            trieCurrent.Nodes[ch].Parent = trieCurrent;
            trieCurrent = trieCurrent.Nodes[ch];

            // When we find an * then the next character is the delimiter where next part of the path begins
            // Store it to use it when looking up the trie to find where the part of the route after param value start.
            if (ch == AsteriskChar && i < route.Length - 1)
            {
                trieCurrent.Delimiter = route[i + 1];
            }
        }

        var httpMethod = routeMetadata.MethodType.ToUpperInvariant();
        for (int j = 0; j < httpMethod.Length; j++)
        {
            char ch = httpMethod[j];
            if (ch >= Constants.ASCIICharCount)
            {
                return;
            }

            trieCurrent.Nodes[ch] ??= new();
            trieCurrent.YoungestChild = ch < trieCurrent.YoungestChild ? ch : trieCurrent.YoungestChild;
            trieCurrent.EldestChild = ch > trieCurrent.EldestChild ? ch : trieCurrent.EldestChild;
            trieCurrent.ChildNodesCount = (byte)(trieCurrent.EldestChild - trieCurrent.YoungestChild + 1);
            trieCurrent.Nodes[ch].Parent = trieCurrent;
            trieCurrent = trieCurrent.Nodes[ch];
        }

        trieCurrent.RequestMetadata = routeMetadata;
    }

    private static Dictionary<string, ProcessedMetadata> ProcessDownstreamDependencyMetadata(Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap)
    {
        Dictionary<string, ProcessedMetadata> finalArrayDict = new();
        foreach (var dep in dependencyTrieMap)
        {
            var finalArray = ProcessDownstreamDependencyMetadataInternal(dep.Value);
            finalArrayDict.Add(dep.Key, finalArray);
        }

        return finalArrayDict;
    }

    // This method has 100% coverage but there is some issue with the code coverage tool in the CI pipeline which makes it
    // buggy and complain about some parts the code in this method as not covered. If you make changes to this method, please
    // remove the ExlcudeCodeCoverage attribute and ensure it's covered fully using local runs and enable it back before
    // pushing the change to PR.
    [ExcludeFromCodeCoverage]
    private static ProcessedMetadata ProcessDownstreamDependencyMetadataInternal(RequestMetadataTrieNode requestMetadataTrieRoot)
    {
        Queue<RequestMetadataTrieNode> queue = new();
        queue.Enqueue(requestMetadataTrieRoot);
        int finalArraySize = 0;
        int requestMetadataArraySize = 1;
        while (queue.Count > 0)
        {
            var trieNode = queue.Dequeue();
            finalArraySize += trieNode.ChildNodesCount;
            for (int i = 0; i < Constants.ASCIICharCount; i++)
            {
                var node = trieNode.Nodes[i];
                if (node != null)
                {
                    if (node.RequestMetadata != null)
                    {
                        requestMetadataArraySize++;
                    }

                    queue.Enqueue(node);
                }
            }
        }

        var requestMetadatas = new RequestMetadata[requestMetadataArraySize + 1];
        requestMetadatas[0] = requestMetadataTrieRoot.RequestMetadata!;

        var processedNodes = new FrozenRequestMetadataTrieNode[finalArraySize + 1];
        processedNodes[0] = new FrozenRequestMetadataTrieNode
        {
            ChildStartIndex = 1,
            YoungestChild = requestMetadataTrieRoot.YoungestChild,
            ChildNodesCount = 1,
            RequestMetadataEntryIndex = 0
        };

        queue.Enqueue(requestMetadataTrieRoot);
        int processedNodeIndex = 1;
        int childStartIndex = 2;
        int requestMetadataIndex = 0;
        while (queue.Count > 0)
        {
            var trieNode = queue.Dequeue();
            for (int i = 0; i < Constants.ASCIICharCount; i++)
            {
                var node = trieNode.Nodes[i];
                if (node != null)
                {
                    var d = new FrozenRequestMetadataTrieNode
                    {
                        ChildStartIndex = childStartIndex,
                        Delimiter = node.Delimiter,
                        ChildNodesCount = node.ChildNodesCount,
                        YoungestChild = node.YoungestChild
                    };

                    if (node.RequestMetadata != null)
                    {
                        d.RequestMetadataEntryIndex = ++requestMetadataIndex;
                        requestMetadatas[requestMetadataIndex] = node.RequestMetadata;
                    }

                    processedNodes[processedNodeIndex + i - node.Parent!.YoungestChild] = d;

                    childStartIndex += node.ChildNodesCount;

                    queue.Enqueue(node);
                }
            }

            processedNodeIndex += trieNode.ChildNodesCount;
        }

        return new ProcessedMetadata { Nodes = processedNodes, RequestMetadatas = requestMetadatas };
    }

    private static FrozenRequestMetadataTrieNode? GetChildNode(char ch, FrozenRequestMetadataTrieNode node, ProcessedMetadata routeMetadataRoot)
    {
        bool isValid = ch >= node.YoungestChild && ch <= (node.YoungestChild + node.ChildNodesCount);
        if (isValid)
        {
            return routeMetadataRoot.Nodes![node.ChildStartIndex + ch - node.YoungestChild];
        }

        return null;
    }

    private void AddDependency(IDownstreamDependencyMetadata downstreamDependencyMetadata, Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap)
    {
        foreach (var hostNameSuffix in downstreamDependencyMetadata.UniqueHostNameSuffixes)
        {
            // Add hostname to hostname suffix trie
            AddHostnameToTrie(hostNameSuffix, downstreamDependencyMetadata.DependencyName);
        }

        foreach (var routeMetadata in downstreamDependencyMetadata.RequestMetadata)
        {
            routeMetadata.DependencyName = downstreamDependencyMetadata.DependencyName;

            // Add route metadata to the route per dependency trie
            AddRouteToTrie(routeMetadata, dependencyTrieMap);
        }
    }

    private void AddHostnameToTrie(string hostNameSuffix, string dependencyName)
    {
        hostNameSuffix = hostNameSuffix.ToUpperInvariant();
        var trieCurrent = _hostSuffixTrieRoot;
        for (int i = hostNameSuffix.Length - 1; i >= 0; i--)
        {
            char ch = hostNameSuffix[i];
            if (ch >= Constants.ASCIICharCount)
            {
                return;
            }

            trieCurrent.Nodes[ch] ??= new HostSuffixTrieNode();
            trieCurrent = trieCurrent.Nodes[ch];
        }

        trieCurrent.DependencyName = dependencyName;
    }

    private string GetHostDependencyName(string host)
    {
        string dependencyName = string.Empty;
        var trieCurrent = _hostSuffixTrieRoot;
        for (int i = host.Length - 1; i >= 0; i--)
        {
            char ch = host[i];
            if (ch >= Constants.ASCIICharCount)
            {
                return string.Empty;
            }

            ch = _toUpper[ch];
            if (trieCurrent.Nodes[ch] == null)
            {
                break;
            }

            trieCurrent = trieCurrent.Nodes[ch];
            if (!string.IsNullOrEmpty(trieCurrent.DependencyName))
            {
                dependencyName = trieCurrent.DependencyName;
            }
        }

        return dependencyName;
    }

    private RequestMetadata? GetRequestMetadataInternal(string httpMethod, string requestPath, string dependencyName)
    {
        if (!_frozenProcessedMetadataMap.TryGetValue(dependencyName, out var routeMetadataTrieRoot))
        {
            return null;
        }

        var trieCurrent = routeMetadataTrieRoot.Nodes[0];
        var lastStartNode = trieCurrent;
        var requestPathEndIndex = requestPath.Length;
        for (int i = 0; i < requestPathEndIndex; i++)
        {
            char ch = _toUpper[requestPath[i]];
            var childNode = GetChildNode(ch, trieCurrent, routeMetadataTrieRoot);
            if (childNode == null)
            {
                trieCurrent = lastStartNode;
                var asteriskChildNode = GetChildNode(AsteriskChar, trieCurrent, routeMetadataTrieRoot);
                if (asteriskChildNode == null)
                {
                    break;
                }

                // advance the trie to next delimiter
                trieCurrent = asteriskChildNode;

                if (trieCurrent.Delimiter == Constants.DefaultRouteEndDelim)
                {
                    break;
                }

                var nextDelimiterIndex = requestPath.IndexOf(trieCurrent.Delimiter, i, requestPathEndIndex - i);

                // if we reached end of the request path or end of trie, break
                var delimChildNode = GetChildNode(trieCurrent.Delimiter, trieCurrent, routeMetadataTrieRoot);
                if (nextDelimiterIndex == -1 || delimChildNode == null)
                {
                    break;
                }

                // Advance i to the next separator index
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                i = nextDelimiterIndex;
#pragma warning restore S127 // "for" loop stop conditions should be invariant

                trieCurrent = delimChildNode;

                // Set lastStartNode to trieCurrent
                lastStartNode = trieCurrent;

                continue;
            }

            trieCurrent = childNode;
            if (GetChildNode(AsteriskChar, trieCurrent, routeMetadataTrieRoot) != null)
            {
                lastStartNode = trieCurrent;
            }
        }

        // Now that path is found, Find the method type branch of the trie
        for (int j = 0; j < httpMethod.Length; j++)
        {
            char ch = _toUpper[httpMethod[j]];
            var childNode = GetChildNode(ch, trieCurrent, routeMetadataTrieRoot);
            if (childNode == null)
            {
                return null;
            }

            trieCurrent = childNode;
        }

        return trieCurrent.RequestMetadataEntryIndex == -1 ? null : routeMetadataTrieRoot.RequestMetadatas[trieCurrent.RequestMetadataEntryIndex];
    }
}
