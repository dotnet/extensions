// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Formats HTTP request paths using route templates with sensitive parameters optionally redacted.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Telemetry, UrlFormat = DiagnosticIds.UrlFormat)]
[SuppressMessage("Minor Code Smell", "S1694:An abstract class should have both abstract and concrete methods", Justification = "Abstract for extensibility; no abstract members required now.")]
public abstract class HttpRouteFormatter
{
    private const char ForwardSlashSymbol = '/';

#if NET
    private const char ForwardSlash = ForwardSlashSymbol;
#else
#pragma warning disable IDE1006 // Naming Styles
    private static readonly char[] ForwardSlash = new[] { ForwardSlashSymbol };
#pragma warning restore IDE1006 // Naming Styles
#endif

    private readonly HttpRouteParser _httpRouteParser;
    private readonly IRedactorProvider _redactorProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRouteFormatter"/> class.
    /// </summary>
    /// <param name="httpRouteParser">The route parser.</param>
    /// <param name="redactorProvider">The redactor provider used to redact sensitive parameters.</param>
    protected HttpRouteFormatter(HttpRouteParser httpRouteParser, IRedactorProvider redactorProvider)
    {
        _httpRouteParser = httpRouteParser;
        _redactorProvider = redactorProvider;
    }

    /// <summary>
    /// Format the http path using the route template with sensitive parameters redacted.
    /// </summary>
    /// <param name="httpRoute">Http request route template.</param>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <returns>Returns formatted path with sensitive parameter values redacted.</returns>
    public virtual string Format(string httpRoute, string httpPath, HttpRouteParameterRedactionMode redactionMode, IReadOnlyDictionary<string, DataClassification> parametersToRedact)
    {
        ParsedRouteSegments routeSegments = _httpRouteParser.ParseRoute(httpRoute);
        return Format(routeSegments, httpPath, redactionMode, parametersToRedact);
    }

    /// <summary>
    /// Format the http path using the route template with sensitive parameters redacted.
    /// </summary>
    /// <param name="routeSegments">Http request's route segments.</param>
    /// <param name="httpPath">Http request's absolute path.</param>
    /// <param name="redactionMode">Strategy to decide how parameters are redacted.</param>
    /// <param name="parametersToRedact">Dictionary of parameters with their data classification that needs to be redacted.</param>
    /// <returns>Returns formatted path with sensitive parameter values redacted.</returns>
    public virtual string Format(
        in ParsedRouteSegments routeSegments,
        string httpPath,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact)
    {
        if (httpPath is null)
        {
            return string.Empty;
        }

        if (parametersToRedact is null ||
            routeSegments.ParameterCount == 0 ||
            !IsRedactionRequired(routeSegments, redactionMode, parametersToRedact))
        {
            return httpPath.Trim(ForwardSlash);
        }

        ReadOnlySpan<char> httpPathAsSpan = httpPath.AsSpan().TrimStart(ForwardSlash);
        StringBuilder pathStringBuilder = PoolFactory.SharedStringBuilderPool.Get();

        try
        {
            int offset = 0;

            for (int i = 0; i < routeSegments.Segments.Length; i++)
            {
                Segment segment = routeSegments.Segments[i];

                if (segment.IsParam)
                {
                    string parameterContent = segment.Content;
                    int parameterTemplateLength = parameterContent.Length + 2;

                    int startIndex = segment.Start + offset;

                    // If we exceed a length of the http path it means that the appropriate http route
                    // has optional parameters or parameters with default values, and these parameters
                    // are omitted in the http path. In this case we stop processing and return resulting
                    // http path.
                    if (startIndex >= httpPathAsSpan.Length)
                    {
                        break;
                    }

                    int length;

                    if (i < routeSegments.Segments.Length - 1)
                    {
                        length = httpPathAsSpan.Slice(startIndex).IndexOf(routeSegments.Segments[i + 1].Content[0]);
                    }
                    else
                    {
                        length = httpPathAsSpan.Slice(startIndex).IndexOf(ForwardSlash);
                    }

                    if (length == -1)
                    {
                        length = httpPathAsSpan.Slice(startIndex).Length;
                    }

                    offset += length - parameterTemplateLength;

                    FormatParameter(httpPathAsSpan, segment, startIndex, length, redactionMode, parametersToRedact, pathStringBuilder);
                }
                else
                {
                    _ = pathStringBuilder.Append(segment.Content);
                }
            }

            RemoveTrailingForwardSlash(pathStringBuilder);

            return pathStringBuilder.ToString();
        }
        finally
        {
            PoolFactory.SharedStringBuilderPool.Return(pathStringBuilder);
        }
    }

    private static bool IsRedactionRequired(
        in ParsedRouteSegments routeSegments, HttpRouteParameterRedactionMode redactionMode, IReadOnlyDictionary<string, DataClassification> parametersToRedact)
    {
        if (redactionMode == HttpRouteParameterRedactionMode.None)
        {
            return false;
        }

        foreach (Segment segment in routeSegments.Segments)
        {
            if (!segment.IsParam)
            {
                continue;
            }

            switch (redactionMode)
            {
                case HttpRouteParameterRedactionMode.Strict:
                {
                    // If no data class exists for a parameter, and the parameter is not a well known parameter, then we redact it.
                    // If data class exists, and it's anything other than DataClassification.None, then also we redact it.
                    if ((!parametersToRedact.TryGetValue(segment.ParamName, out DataClassification classification) &&
                         !Segment.IsKnownUnredactableParameter(segment.ParamName)) ||
                        classification != DataClassification.None)
                    {
                        return true;
                    }

                    break;
                }

                case HttpRouteParameterRedactionMode.Loose:
                {
                    // If data class exists for a parameter, and it's anything other than DataClassification.None, then we redact it.
                    if (parametersToRedact.TryGetValue(segment.ParamName, out DataClassification classification) && classification != DataClassification.None)
                    {
                        return true;
                    }

                    break;
                }

                default:
                    throw new InvalidOperationException(TelemetryCommonExtensions.UnsupportedEnumValueExceptionMessage);
            }
        }

        return false;
    }

    private static void RemoveTrailingForwardSlash(StringBuilder formattedHttpPath)
    {
        if (formattedHttpPath.Length <= 1)
        {
            return;
        }

        int lastCharIndex = formattedHttpPath.Length - 1;
        if (formattedHttpPath[lastCharIndex] == ForwardSlashSymbol)
        {
            _ = formattedHttpPath.Remove(lastCharIndex, 1);
        }
    }

    private void FormatParameter(
        ReadOnlySpan<char> httpPath,
        in Segment segment,
        int startIndex,
        int length,
        HttpRouteParameterRedactionMode redactionMode,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        StringBuilder outputBuffer)
    {
        switch (redactionMode)
        {
            case HttpRouteParameterRedactionMode.Strict:
                FormatParameterInStrictMode(httpPath, segment, startIndex, length, parametersToRedact, outputBuffer);
                return;
            case HttpRouteParameterRedactionMode.Loose:
                FormatParameterInLooseMode(httpPath, segment, startIndex, length, parametersToRedact, outputBuffer);
                break;
        }
    }

    private void FormatParameterInStrictMode(
        ReadOnlySpan<char> httpPath,
        Segment httpRouteSegment,
        int startIndex,
        int length,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        StringBuilder outputBuffer)
    {
        if (parametersToRedact.TryGetValue(httpRouteSegment.ParamName, out var classification))
        {
            if (classification != DataClassification.None)
            {
                Redactor redactor = _redactorProvider.GetRedactor(classification);
                _ = outputBuffer.AppendRedacted(redactor, httpPath.Slice(startIndex, length));
            }
            else
            {
#if NET
                _ = outputBuffer.Append(httpPath.Slice(startIndex, length));
#else
                _ = outputBuffer.Append(httpPath.Slice(startIndex, length).ToString());
#endif
            }
        }
        else if (Segment.IsKnownUnredactableParameter(httpRouteSegment.ParamName))
        {
#if NET
            _ = outputBuffer.Append(httpPath.Slice(startIndex, length));
#else
            _ = outputBuffer.Append(httpPath.Slice(startIndex, length).ToString());
#endif
        }
        else
        {
#if NET
            _ = outputBuffer.Append(TelemetryConstants.Redacted.AsSpan());
#else
            _ = outputBuffer.Append(TelemetryConstants.Redacted);
#endif
        }
    }

    private void FormatParameterInLooseMode(
        ReadOnlySpan<char> httpPath,
        Segment httpRouteSegment,
        int startIndex,
        int length,
        IReadOnlyDictionary<string, DataClassification> parametersToRedact,
        StringBuilder outputBuffer)
    {
        if (parametersToRedact.TryGetValue(httpRouteSegment.ParamName, out DataClassification classification)
            && classification != DataClassification.None)
        {
            Redactor redactor = _redactorProvider.GetRedactor(classification);
            _ = outputBuffer.AppendRedacted(redactor, httpPath.Slice(startIndex, length));
        }
        else
        {
#if NET
            _ = outputBuffer.Append(httpPath.Slice(startIndex, length));
#else
            _ = outputBuffer.Append(httpPath.Slice(startIndex, length).ToString());
#endif
        }
    }
}
