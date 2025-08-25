// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Manages downstream dependency metadata and resolves route information for outgoing HTTP requests.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
[SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods", Justification = "Want abstract class for extensibility.")]
public abstract class DownstreamDependencyMetadataManager
{
    private readonly struct ProcessedMetadata
    {
        public FrozenRequestMetadataTrieNode[] Nodes { get; init; }
        public RequestMetadata[] RequestMetadatas { get; init; }
    }

    private const char AsteriskChar = '*';
    private static readonly Regex _routeRegex = DownstreamDependencyMetadataManagerRegex.MakeRouteRegex();
    private static readonly char[] _toUpper = MakeToUpperArray();

    private readonly HostSuffixTrieNode _hostSuffixTrieRoot = new();
    private readonly FrozenDictionary<string, ProcessedMetadata> _frozenProcessedMetadataMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownstreamDependencyMetadataManager"/> class.
    /// </summary>
    /// <param name="downstreamDependencyMetadata">A collection of downstream dependency metadata.</param>
    protected DownstreamDependencyMetadataManager(IEnumerable<IDownstreamDependencyMetadata> downstreamDependencyMetadata)
    {
        _ = Throw.IfNull(downstreamDependencyMetadata);

        Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap = [];
        foreach (IDownstreamDependencyMetadata dependency in downstreamDependencyMetadata)
        {
            AddDependency(dependency, dependencyTrieMap);
        }

        _frozenProcessedMetadataMap = ProcessDownstreamDependencyMetadata(dependencyTrieMap).ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets request metadata for the specified HTTP request message.
    /// </summary>
    /// <param name="requestMessage">The HTTP request.</param>
    /// <returns>The resolved <see cref="RequestMetadata"/> if found; otherwise, <see langword="false" />.</returns>
    public virtual RequestMetadata? GetRequestMetadata(HttpRequestMessage requestMessage)
    {
        _ = Throw.IfNull(requestMessage);
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            if (requestMessage.RequestUri == null)
            {
                return null;
            }

            HostSuffixTrieNode? hostMetadata = GetHostMetadata(requestMessage.RequestUri.Host);
            return GetRequestMetadataInternal(requestMessage.Method.Method, requestMessage.RequestUri.AbsolutePath, hostMetadata);
        }
        catch (Exception)
        {
            // Catch exceptions here to avoid impacting services if a bug ever gets introduced in this path.
            return null;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    /// <summary>
    /// Gets request metadata for the specified HTTP web request.
    /// </summary>
    /// <param name="requestMessage">The HTTP web request.</param>
    /// <returns>The resolved <see cref="RequestMetadata"/> if found; otherwise, null.</returns>
    public virtual RequestMetadata? GetRequestMetadata(HttpWebRequest requestMessage)
    {
        _ = Throw.IfNull(requestMessage);
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            HostSuffixTrieNode? hostMetadata = GetHostMetadata(requestMessage.RequestUri.Host);
            return GetRequestMetadataInternal(requestMessage.Method, requestMessage.RequestUri.AbsolutePath, hostMetadata);
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

        RequestMetadataTrieNode? trieCurrent = routeMetadataTrieRoot;
        trieCurrent.Parent = trieCurrent;

        string route = routeMetadata.RequestRoute;
        if (!string.IsNullOrEmpty(route))
        {
            var routeSpan = route.AsSpan();
            if (routeSpan.StartsWith("//".AsSpan()))
            {
                routeSpan = routeSpan.Slice(1);
            }

            if (routeSpan.Length > 1 && routeSpan[routeSpan.Length - 1] == '/')
            {
                routeSpan = routeSpan.Slice(0, routeSpan.Length - 1);
            }

            if (routeSpan[0] != '/')
            {
#if NET
                route = $"/{routeSpan}";
#else
                route = $"/{routeSpan.ToString()}";
#endif
            }
            else if (routeSpan.Length != route.Length)
            {
                route = routeSpan.ToString();
            }

            route = _routeRegex.Replace(route, "*").ToUpperInvariant();
        }
        else
        {
            route = "/";
        }

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

        string httpMethod = routeMetadata.MethodType.ToUpperInvariant();
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
        Dictionary<string, ProcessedMetadata> finalArrayDict = [];
        foreach (KeyValuePair<string, RequestMetadataTrieNode> dep in dependencyTrieMap)
        {
            ProcessedMetadata finalArray = ProcessDownstreamDependencyMetadataInternal(dep.Value);
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
            RequestMetadataTrieNode trieNode = queue.Dequeue();
            finalArraySize += trieNode.ChildNodesCount;
            for (int i = 0; i < Constants.ASCIICharCount; i++)
            {
                RequestMetadataTrieNode node = trieNode.Nodes[i];
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
            RequestMetadataTrieNode trieNode = queue.Dequeue();
            for (int i = 0; i < Constants.ASCIICharCount; i++)
            {
                RequestMetadataTrieNode node = trieNode.Nodes[i];
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
        bool isValid = ch >= node.YoungestChild && ch <= node.YoungestChild + node.ChildNodesCount;
        return isValid ? routeMetadataRoot.Nodes[node.ChildStartIndex + ch - node.YoungestChild] : null;
    }

    private void AddDependency(IDownstreamDependencyMetadata downstreamDependencyMetadata, Dictionary<string, RequestMetadataTrieNode> dependencyTrieMap)
    {
        foreach (string hostNameSuffix in downstreamDependencyMetadata.UniqueHostNameSuffixes)
        {
            // Add hostname to hostname suffix trie
            AddHostnameToTrie(hostNameSuffix, downstreamDependencyMetadata.DependencyName);
        }

        foreach (RequestMetadata routeMetadata in downstreamDependencyMetadata.RequestMetadata)
        {
            routeMetadata.DependencyName = downstreamDependencyMetadata.DependencyName;

            // Add route metadata to the route per dependency trie
            AddRouteToTrie(routeMetadata, dependencyTrieMap);
        }
    }

    private void AddHostnameToTrie(string hostNameSuffix, string dependencyName)
    {
        hostNameSuffix = hostNameSuffix.ToUpperInvariant();
        HostSuffixTrieNode trieCurrent = _hostSuffixTrieRoot;
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
        trieCurrent.RequestMetadata.DependencyName = dependencyName;
        trieCurrent.RequestMetadata.MethodType = string.Empty;
    }

    private HostSuffixTrieNode? GetHostMetadata(string host)
    {
        HostSuffixTrieNode? hostMetadataNode = null;
        HostSuffixTrieNode trieCurrent = _hostSuffixTrieRoot;
        for (int i = host.Length - 1; i >= 0; i--)
        {
            char ch = host[i];
            if (ch >= Constants.ASCIICharCount)
            {
                return null;
            }

            ch = _toUpper[ch];
            if (trieCurrent.Nodes[ch] == null)
            {
                break;
            }

            trieCurrent = trieCurrent.Nodes[ch];
            if (!string.IsNullOrEmpty(trieCurrent.DependencyName))
            {
                hostMetadataNode = trieCurrent;
            }
        }

        return hostMetadataNode;
    }

    private RequestMetadata? GetRequestMetadataInternal(string httpMethod, string requestPath, HostSuffixTrieNode? hostMetadata)
    {
        if (hostMetadata == null)
        {
            return null;
        }

        if (!_frozenProcessedMetadataMap.TryGetValue(hostMetadata.DependencyName, out var routeMetadataTrieRoot))
        {
            return hostMetadata.RequestMetadata;
        }

        ReadOnlySpan<char> requestRouteAsSpan = requestPath.AsSpan();

        if (requestRouteAsSpan.Length > 1)
        {
            if (requestRouteAsSpan[requestRouteAsSpan.Length - 1] == '/')
            {
                requestRouteAsSpan = requestRouteAsSpan.Slice(0, requestRouteAsSpan.Length - 1);
            }

            if (requestRouteAsSpan.StartsWith("//".AsSpan(), StringComparison.OrdinalIgnoreCase))
            {
                requestRouteAsSpan = requestRouteAsSpan.Slice(1);
            }
        }

        FrozenRequestMetadataTrieNode trieCurrent = routeMetadataTrieRoot.Nodes[0];
        FrozenRequestMetadataTrieNode lastStartNode = trieCurrent;
        int requestPathEndIndex = requestRouteAsSpan.Length;
        for (int i = 0; i < requestPathEndIndex; i++)
        {
            char ch = _toUpper[requestRouteAsSpan[i]];
            FrozenRequestMetadataTrieNode? childNode = GetChildNode(ch, trieCurrent, routeMetadataTrieRoot);
            if (childNode == null)
            {
                trieCurrent = lastStartNode;
                FrozenRequestMetadataTrieNode? asteriskChildNode = GetChildNode(AsteriskChar, trieCurrent, routeMetadataTrieRoot);
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

                // we add i to the index, because the index returned from ReadOnlySpan<char> is the index in the new slice, not in the original slice.
                int nextDelimiterIndex = requestRouteAsSpan.Slice(i, requestPathEndIndex - i).IndexOf(trieCurrent.Delimiter) + i;

                // if we reached end of the request path or end of trie, break
                FrozenRequestMetadataTrieNode? delimChildNode = GetChildNode(trieCurrent.Delimiter, trieCurrent, routeMetadataTrieRoot);
                if (nextDelimiterIndex == i - 1 || delimChildNode == null)
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
            FrozenRequestMetadataTrieNode? childNode = GetChildNode(ch, trieCurrent, routeMetadataTrieRoot);
            if (childNode == null)
            {
                // Return the default request metadata for the host which
                // contains only the dependency name, but no other route/request info.
                return hostMetadata.RequestMetadata;
            }

            trieCurrent = childNode;
        }

        return trieCurrent.RequestMetadataEntryIndex == -1 ?
            hostMetadata.RequestMetadata : // Return the default request metadata for this host if no matching route is found.
            routeMetadataTrieRoot.RequestMetadatas[trieCurrent.RequestMetadataEntryIndex];
    }
}
