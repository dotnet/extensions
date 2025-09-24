// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// HTTP request route parser.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
[SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods", Justification = "Abstract for extensibility; no abstract members required now.")]
public abstract class HttpRouteParser
{
#if NET
    private const char ForwardSlash = '/';
#else
#pragma warning disable IDE1006 // Naming Styles
    private static readonly char[] ForwardSlash = new[] { '/' };
#pragma warning restore IDE1006 // Naming Styles
#endif

    private readonly IRedactorProvider _redactorProvider;
    private readonly ConcurrentDictionary<string, ParsedRouteSegments> _routeTemplateSegmentsCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRouteParser"/> class.
    /// </summary>
    /// <param name="redactorProvider">Redactor provider to use for getting redactors for privacy data.</param>
    protected HttpRouteParser(IRedactorProvider redactorProvider)
    {
        _redactorProvider = redactorProvider;
    }

    /// <summary>
    /// Extract parameters values from the http request path.
    /// </summary>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="routeSegments">Route segments containing text and parameter segments of the route.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <param name="httpRouteParameters">Output array where parameters will be stored. Caller must provide the array with enough capacity to hold all parameters in route segment.</param>
    /// <returns><see langword="true" /> if parameters were extracted successfully, <see langword="false" /> otherwise.</returns>
    public virtual bool TryExtractParameters(
        string httpPath,
        in ParsedRouteSegments routeSegments,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        ref HttpRouteParameter[] httpRouteParameters)
    {
        int paramCount = routeSegments.ParameterCount;

        if (httpRouteParameters is null || httpRouteParameters.Length < paramCount)
        {
            return false;
        }

        ReadOnlySpan<char> httpPathAsSpan = httpPath.AsSpan();
        httpPathAsSpan = httpPathAsSpan.TrimStart(ForwardSlash);

        if (paramCount <= 0)
        {
            return true;
        }

        int offset = 0;
        int index = 0;

        foreach (Segment segment in routeSegments.Segments)
        {
            if (!segment.IsParam)
            {
                continue;
            }

            int startIndex = segment.Start + offset;

            // If we exceed a length of the http path it means that the appropriate http route
            // has optional parameters or parameters with default values, and these parameters
            // are omitted in the http path. In this case we return a default value of the
            // omitted parameter.
            string parameterValue = segment.DefaultValue;

            bool isRedacted = false;

            if (startIndex < httpPathAsSpan.Length)
            {
                string parameterContent = segment.Content;
                int parameterTemplateLength = parameterContent.Length + 2;

                int length = httpPathAsSpan.Slice(startIndex).IndexOf(ForwardSlash);

                if (segment.IsCatchAll || length == -1)
                {
                    length = httpPathAsSpan.Slice(startIndex).Length;
                }

                offset += length - parameterTemplateLength;

                parameterValue = GetRedactedParameterValue(httpPathAsSpan, segment, startIndex, length, redactionMode, parametersToRedact, ref isRedacted);
            }

            httpRouteParameters[index++] = new HttpRouteParameter(segment.ParamName, parameterValue, isRedacted);
        }

        return true;
    }

    /// <summary>
    /// Parses http route and breaks it into text and parameter segments.
    /// </summary>
    /// <param name="httpRoute">Http request's route template.</param>
    /// <returns>Returns text and parameter segments of route.</returns>
    public virtual ParsedRouteSegments ParseRoute(string httpRoute)
    {
        return _routeTemplateSegmentsCache.GetOrAdd(httpRoute, _ =>
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
        int paramNameStart = start + 1;
        bool catchAllParamFound = false;
        int defaultValueStart = PositionNotFound;

        char ch;

        while ((ch = httpRoute[pos]) != '}')
        {
            switch (ch)
            {
                // The segment has a default value '='. The character indicates
                // that we've met the end of the segment's parameter name and
                // the start of the default value.
                case '=':
                {
                    if (paramNameEnd == PositionNotFound)
                    {
                        paramNameEnd = pos;
                    }

                    defaultValueStart = pos + 1;
                    break;
                }

                // The segment is optional '?' or has a constraint ':'.
                // When we meet one of the above characters it indicates
                // that we've met the end of the segment's parameter name.
                case '?':
                case ':':
                {
                    if (paramNameEnd == PositionNotFound)
                    {
                        paramNameEnd = pos;
                    }

                    break;
                }

                // The segment has '*' catch all parameter.
                // When we meet the character it indicates param start position needs to be adjusted, so that we capture 'param' instead of '*param'
                // *param can only appear after opening curly brace and position needs to be adjusted only once
                default:
                {
                    if (!catchAllParamFound && ch == '*' && pos > 0 && httpRoute[pos - 1] == '{')
                    {
                        paramNameStart++;

                        // Catch all parameters can start with one or two '*' characters.
                        if (httpRoute[paramNameStart] == '*')
                        {
                            paramNameStart++;
                        }

                        catchAllParamFound = true;
                    }

                    break;
                }
            }

            pos++;
        }

        // Throw an ArgumentException if the segment is a catch-all parameter and not the last segment.
        // The current position should be either the end of the route or the second to last position followed by a '/'.
        if (catchAllParamFound)
        {
            bool isLastPosition = pos == httpRoute.Length - 1;
            bool isSecondToLastPosition = pos == httpRoute.Length - 2;

            if (!(isLastPosition || (isSecondToLastPosition && httpRoute[pos + 1] == '/')))
            {
                Throw.ArgumentException(nameof(httpRoute), "A catch-all parameter must be the last segment in the route.");
            }
        }

        string content = GetSegmentContent(httpRoute, paramNameStart, pos);
        string paramName = paramNameEnd == PositionNotFound
            ? content
            : GetSegmentContent(httpRoute, paramNameStart, paramNameEnd);
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
            defaultValue: defaultValue,
            isCatchAll: catchAllParamFound);
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
            if (classification == DataClassification.None)
            {
                return httpPathAsSpan.Slice(startIndex, length).ToString();
            }

            Redactor redactor = _redactorProvider.GetRedactor(classification);
            isRedacted = true;

            return redactor.Redact(httpPathAsSpan.Slice(startIndex, length));

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
        if (!parametersToRedact.TryGetValue(segment.ParamName, out DataClassification classification)
            || classification == DataClassification.None)
        {
            return httpPathAsSpan.Slice(startIndex, length).ToString();
        }

        Redactor redactor = _redactorProvider.GetRedactor(classification);
        isRedacted = true;

        return redactor.Redact(httpPathAsSpan.Slice(startIndex, length));

    }
}

