// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.Extensions.Http.Diagnostics;

internal sealed class HttpRouteParser : IHttpRouteParser
{
#if NETCOREAPP3_1_OR_GREATER
    private const char ForwardSlash = '/';
#else
#pragma warning disable IDE1006 // Naming Styles
    private static readonly char[] ForwardSlash = new[] { '/' };
#pragma warning restore IDE1006 // Naming Styles
#endif

    private readonly IRedactorProvider _redactorProvider;
    private readonly ConcurrentDictionary<string, ParsedRouteSegments> _routeTemplateSegmentsCache = new();

    public HttpRouteParser(IRedactorProvider redactorProvider)
    {
        _redactorProvider = redactorProvider;
    }

    public bool TryExtractParameters(
        string httpPath,
        in ParsedRouteSegments routeSegments,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref HttpRouteParameter[] httpRouteParameters)
    {
        int paramCount = routeSegments.ParameterCount;

        if (httpRouteParameters == null || httpRouteParameters.Length < paramCount)
        {
            return false;
        }

        var httpPathAsSpan = httpPath.AsSpan();
        httpPathAsSpan = httpPathAsSpan.TrimStart(ForwardSlash);

        if (paramCount > 0)
        {
            int offset = 0;
            int index = 0;

            foreach (Segment segment in routeSegments.Segments)
            {
                if (segment.IsParam)
                {
                    var startIndex = segment.Start + offset;

                    string parameterValue;
                    bool isRedacted = false;

                    if (startIndex < httpPathAsSpan.Length)
                    {
                        var parameterContent = segment.Content;
                        var parameterTemplateLength = parameterContent.Length + 2;
                        var length = httpPathAsSpan.Slice(startIndex).IndexOf(ForwardSlash);

                        if (length == -1)
                        {
                            length = httpPathAsSpan.Slice(startIndex).Length;
                        }

                        offset += length - parameterTemplateLength;

                        parameterValue = GetRedactedParameterValue(httpPathAsSpan, segment, startIndex, length, redactionMode, parametersToRedact, ref isRedacted);
                    }

                    // If we exceed a length of the http path it means that the appropriate http route
                    // has optional parameters or parameters with default values, and these parameters
                    // are omitted in the http path. In this case we return a default value of the
                    // omitted parameter.
                    else
                    {
                        parameterValue = segment.DefaultValue;
                    }

                    httpRouteParameters[index++] = new HttpRouteParameter(segment.ParamName, parameterValue, isRedacted);
                }
            }
        }

        return true;
    }

    public ParsedRouteSegments ParseRoute(string httpRoute)
    {
        return _routeTemplateSegmentsCache.GetOrAdd(httpRoute, httpRoute =>
        {
            httpRoute = httpRoute.TrimStart(ForwardSlash);

            var pos = 0;
            var len = httpRoute.Length;
            var start = 0;
            char ch;

            var segments = new List<Segment>();

            while (pos < len)
            {
                ch = httpRoute[pos];

                // Start of a parameter segment.
                if (ch == '{')
                {
                    // End of the current text segment.
                    if (pos > start)
                    {
                        segments.Add(new Segment(
                            start: start,
                            end: pos,
                            content: GetSegmentContent(httpRoute, start, pos),
                            isParam: false));
                    }

                    segments.Add(GetParameterSegment(httpRoute, ref pos));
                    start = pos + 1;
                }

                // Start of the query parameters sections.
                else if (ch == '?')
                {
                    // Remove the query parameters from the template.
                    httpRoute = httpRoute.Substring(0, pos);
                    break;
                }

                pos++;
            }

            // End the last text segment if any.
            if (start < pos)
            {
                segments.Add(new Segment(
                    start: start,
                    end: pos,
                    content: GetSegmentContent(httpRoute, start, pos),
                    isParam: false));
            }

            return new ParsedRouteSegments(httpRoute, segments.ToArray());
        });
    }

    private static Segment GetParameterSegment(string httpRoute, ref int pos)
    {
        const int PositionNotFound = -1;

        int start = pos++;
        int paramNameEnd = PositionNotFound;
        int defaultValueStart = PositionNotFound;

        char ch;

        while ((ch = httpRoute[pos]) != '}')
        {
            // The segment has a default value '='. The character indicates
            // that we've met the end of the segment's parameter name and
            // the start of the default value.
            if (ch == '=')
            {
                if (paramNameEnd == PositionNotFound)
                {
                    paramNameEnd = pos;
                }

                defaultValueStart = pos + 1;
            }

            // The segment is optional '?' or has a constraint ':'.
            // When we meet one of the above characters it indicates
            // that we've met the end of the segment's parameter name.
            else if (ch == '?' || ch == ':')
            {
                if (paramNameEnd == PositionNotFound)
                {
                    paramNameEnd = pos;
                }
            }

            pos++;
        }

        string content = GetSegmentContent(httpRoute, start + 1, pos);
        string paramName = paramNameEnd == PositionNotFound
            ? content
            : GetSegmentContent(httpRoute, start + 1, paramNameEnd);
        string defaultValue = defaultValueStart == PositionNotFound
            ? string.Empty
            : GetSegmentContent(httpRoute, defaultValueStart, pos);

        // Remove the opening and closing curly braces when getting content.
        return new Segment(
            start: start,
            end: pos + 1,
            content: content,
            isParam: true,
            paramName: paramName,
            defaultValue: defaultValue);
    }

    private static string GetSegmentContent(string httpRoute, int start, int end)
    {
        return httpRoute.Substring(start, end - start);
    }

    private string GetRedactedParameterValue(
        ReadOnlySpan<char> httpPath,
        in Segment segment,
        int startIndex,
        int length,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref bool isRedacted)
    {
        return redactionMode switch
        {
            HttpRouteParameterRedactionMode.None => httpPath.Slice(startIndex, length).ToString(),
            HttpRouteParameterRedactionMode.Strict => GetRedactedParameterInStrictMode(httpPath, segment, startIndex, length, parametersToRedact, ref isRedacted),
            HttpRouteParameterRedactionMode.Loose => GetRedactedParameterInLooseMode(httpPath, segment, startIndex, length, parametersToRedact, ref isRedacted),
            _ => throw new InvalidOperationException(TelemetryCommonExtensions.UnsupportedEnumValueExceptionMessage)
        };
    }

    private string GetRedactedParameterInStrictMode(
        ReadOnlySpan<char> httpPathAsSpan,
        Segment segment,
        int startIndex,
        int length,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref bool isRedacted)
    {
        if (parametersToRedact.TryGetValue(segment.ParamName, out DataClassification classification))
        {
            if (classification != DataClassification.None)
            {
                var redactor = _redactorProvider.GetRedactor(classification);
                isRedacted = true;

                return redactor.Redact(httpPathAsSpan.Slice(startIndex, length));
            }

            return httpPathAsSpan.Slice(startIndex, length).ToString();
        }

        if (Segment.IsKnownUnredactableParameter(segment.ParamName))
        {
            return httpPathAsSpan.Slice(startIndex, length).ToString();
        }

        isRedacted = true;

        return TelemetryConstants.Redacted;
    }

    private string GetRedactedParameterInLooseMode(
        ReadOnlySpan<char> httpPathAsSpan,
        Segment segment,
        int startIndex,
        int length,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref bool isRedacted)
    {
        if (parametersToRedact.TryGetValue(segment.ParamName, out DataClassification classification)
            && classification != DataClassification.None)
        {
            var redactor = _redactorProvider.GetRedactor(classification);
            isRedacted = true;

            return redactor.Redact(httpPathAsSpan.Slice(startIndex, length));
        }

        return httpPathAsSpan.Slice(startIndex, length).ToString();
    }
}
