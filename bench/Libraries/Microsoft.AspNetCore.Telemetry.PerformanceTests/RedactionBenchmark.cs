// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Shared.Pools;

namespace Microsoft.AspNetCore.Telemetry.Bench;

[GcServer(true)]
[MinColumn]
[MaxColumn]
[MemoryDiagnoser]
public class RedactionBenchmark
{
    private readonly string _httpPath;
    private readonly Dictionary<string, DataClassification> _routeParameterDataClasses = new();
    private readonly ObjectPool<StringBuilder> _stringBuilderPool;
    private readonly Dictionary<string, object?> _routeValues = new()
    {
        { "userId", "testUserId" },
        { "chatId", "testChatId" },
    };

    public RedactionBenchmark()
    {
        _routeParameterDataClasses.Add("userId", FakeClassifications.PrivateData);
        _routeParameterDataClasses.Add("chatId", FakeClassifications.PrivateData);

        _httpPath = "/users/{userId}/chats/{chatId}/test1/test2/{userId}";
        _stringBuilderPool = PoolFactory.CreateStringBuilderPool();
    }

    private static RouteSegment[] GetRouteSegments(string httpRoute)
    {
        var routeSegments = new List<RouteSegment>();

        int startIndex = 0;
        while (startIndex < httpRoute.Length)
        {
            var startIndexOfParam = httpRoute.IndexOf('{', startIndex);
            if (startIndexOfParam == -1)
            {
                // We have reached to the end of the segment, no more parameters
                routeSegments.Add(new RouteSegment(httpRoute.Substring(startIndex), false));
                break;
            }

            var endIndexOfParam = httpRoute.IndexOf('}', startIndexOfParam);

            var routeNonParamameterSegment = httpRoute.Substring(startIndex, startIndexOfParam - startIndex);
            var routeParameterSegment = httpRoute.Substring(startIndexOfParam + 1, endIndexOfParam - startIndexOfParam - 1);

            routeSegments.Add(new RouteSegment(routeNonParamameterSegment, false));
            routeSegments.Add(new RouteSegment(routeParameterSegment, true));

            startIndex = endIndexOfParam + 1;
        }

        return routeSegments.ToArray();
    }

    [Benchmark]
    public void RedactHttpPathStringBuilderNETStd()
    {
        Span<char> destinationBuffer = stackalloc char[256];
        var startIndex = 0;
        var span = _httpPath.AsSpan();
        var isRouteKeyFound = false;
        var pathStringBuilder = _stringBuilderPool.Get();
        ReadOnlySpan<char> segment;
        try
        {
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == '/')
                {
                    segment = span.Slice(startIndex, i - startIndex);

                    foreach (var item in _routeValues)
                    {
                        if (((string)item.Value!).AsSpan().SequenceEqual(segment))
                        {
                            if (_routeParameterDataClasses.TryGetValue(item.Key, out DataClassification classification))
                            {
                                pathStringBuilder.Append(Redact(segment, destinationBuffer));
                                isRouteKeyFound = true;
                            }

                            break;
                        }
                    }

                    if (!isRouteKeyFound)
                    {
                        pathStringBuilder.Append(segment);
                    }

                    if (i < span.Length)
                    {
                        _ = pathStringBuilder.Append('/');
                    }

                    startIndex = i + 1;

                    isRouteKeyFound = true;
                }
            }

            _ = pathStringBuilder.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(pathStringBuilder);
        }
    }

    [Benchmark]
    public void RedactHttpPathStringBuilderOptimizedForSpeedNETStd()
    {
        Span<char> destinationBuffer = stackalloc char[256];
        var startIndex = 0;
        var span = _httpPath.AsSpan();
        var pathStringBuilder = _stringBuilderPool.Get();

        var newDict = new Dictionary<string, string>(_routeValues.Count);
        foreach (var item in _routeValues)
        {
            if (item.Value is not null)
            {
                newDict.Add((string)item.Value, item.Key);
            }
        }

        try
        {
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == '/')
                {
                    var segment = span.Slice(startIndex, i - startIndex).ToString();
                    if (newDict.TryGetValue(segment, out var value) &&
                        _routeParameterDataClasses.TryGetValue(value, out DataClassification classification))
                    {
                        _ = pathStringBuilder.Append(Redact(segment, destinationBuffer));
                    }
                    else
                    {
                        _ = pathStringBuilder.Append(segment);
                    }

                    if (i < span.Length)
                    {
                        _ = pathStringBuilder.Append('/');
                    }

                    startIndex = i + 1;
                }
            }

            _ = pathStringBuilder.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(pathStringBuilder);
        }
    }

    [Benchmark]
    public void RedactHttpPathUsingIndexOfNETStd()
    {
        Span<char> destinationBuffer = stackalloc char[256];
        var span = _httpPath.AsSpan();
        var isRouteKeyFound = false;
        var pathStringBuilder = _stringBuilderPool.Get();
        try
        {
            var startIndex = 0;
            var endIndex = 0;
            var segment = span;

            while (startIndex < span.Length)
            {
                _ = pathStringBuilder.Append('/');
                startIndex = span.Slice(startIndex).IndexOf('/') + startIndex;
                endIndex = span.Slice(startIndex + 1).IndexOf('/') + startIndex;
                if (endIndex < startIndex)
                {
                    endIndex = span.Length - 1;
                }

                segment = span.Slice(startIndex + 1, endIndex - startIndex);
                if (segment.Length > 0)
                {
                    foreach (var item in _routeValues)
                    {
                        if (((string)item.Value!).AsSpan().SequenceEqual(segment))
                        {
                            if (_routeParameterDataClasses.TryGetValue(item.Key, out DataClassification classification))
                            {
                                _ = pathStringBuilder.Append(Redact(segment, destinationBuffer));
                                isRouteKeyFound = true;
                            }

                            break;
                        }
                    }

                    if (!isRouteKeyFound)
                    {
                        _ = pathStringBuilder.Append(segment);
                    }
                }

                startIndex = endIndex + 1;

                isRouteKeyFound = false;
            }

            _ = pathStringBuilder.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(pathStringBuilder);
        }
    }

    private static ReadOnlySpan<char> Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        return destination.Slice(0, source.Length);
    }

    private static int[] GetPathSegments(ReadOnlySpan<char> httpPath)
    {
        var numSegments = 0;
        for (var i = 0; i < httpPath.Length; i++)
        {
            if (httpPath[i] == '/')
            {
                numSegments++;
            }
        }

        var routeSegments = new int[numSegments + 1];
        var j = 0;
        for (var i = 0; i < httpPath.Length; i++)
        {
            if (httpPath[i] == '/')
            {
                routeSegments[j] = i + 1;
                j++;
            }
        }

        routeSegments[numSegments] = httpPath.Length + 1;
        return routeSegments;
    }

    [Benchmark]
    public void RedactHttpPathWithSegmentsNETStd()
    {
        Span<char> destinationBuffer = stackalloc char[256];
        var span = _httpPath.AsSpan();
        var segments = GetPathSegments(span);
        var isRouteKeyFound = false;
        var pathStringBuilder = _stringBuilderPool.Get();
        try
        {
            for (int i = 1; i < segments.Length; i++)
            {
                _ = pathStringBuilder.Append('/');
                var segment = span.Slice(segments[i - 1], segments[i] - segments[i - 1] - 1);
                foreach (var item in _routeValues)
                {
                    if (((string)item.Value!).AsSpan().SequenceEqual(segment))
                    {
                        if (_routeParameterDataClasses.TryGetValue(item.Key, out DataClassification classification))
                        {
                            _ = pathStringBuilder.Append(Redact(segment, destinationBuffer));
                            isRouteKeyFound = true;
                        }

                        break;
                    }
                }

                if (!isRouteKeyFound)
                {
                    _ = pathStringBuilder.Append(segment);
                }

                isRouteKeyFound = false;
            }

            _ = pathStringBuilder.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(pathStringBuilder);
        }
    }

    [Benchmark]
    public void RedactHttpRouteNETCore()
    {
        Span<char> destinationBuffer = stackalloc char[256];
        var routeSegments = GetRouteSegments(_httpPath);
        var pathStringBuilder = _stringBuilderPool.Get();
        try
        {
            foreach (var routeSegment in routeSegments)
            {
                if (routeSegment.IsParameter)
                {
                    if (_routeValues.TryGetValue(routeSegment.Segment, out var paramValue))
                    {
                        if (_routeParameterDataClasses.TryGetValue(routeSegment.Segment, out DataClassification classification))
                        {
                            _ = pathStringBuilder.Append(Redact((string)paramValue!, destinationBuffer));
                        }
                        else
                        {
                            _ = pathStringBuilder.Append(paramValue);
                        }
                    }
                }
                else
                {
                    _ = pathStringBuilder.Append(routeSegment.Segment);
                }
            }

            _ = pathStringBuilder.ToString();
        }
        finally
        {
            _stringBuilderPool.Return(pathStringBuilder);
        }
    }
}
