// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Shared.Diagnostics;

[assembly: EnumStrings(typeof(WebExceptionStatus))]
[assembly: EnumStrings(typeof(SocketError))]

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Http exception diagnosis for telemetry.
/// </summary>
internal sealed class HttpExceptionSummaryProvider : IExceptionSummaryProvider
{
    private const int DefaultDescriptionIndex = -1;
    private const string TaskCanceled = "TaskCanceled";
    private const string TaskTimeout = "TaskTimeout";
    private static readonly FrozenDictionary<WebExceptionStatus, int> _webExceptionStatusMap;
    private static readonly FrozenDictionary<SocketError, int> _socketErrorMap;
    private static readonly ImmutableArray<string> _descriptions;

    [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Can't do this since the field values are interdependent")]
    static HttpExceptionSummaryProvider()
    {
        var descriptions = new List<string>
        {
            TaskCanceled,
            TaskTimeout
        };

        var socketErrors = new Dictionary<SocketError, int>();
        foreach (var v in Enum.GetValues(typeof(SocketError)))
        {
            var socketError = (SocketError)v!;
            var name = socketError.ToInvariantString();

            socketErrors[socketError] = descriptions.Count;
            descriptions.Add(name);
        }

        var webStatuses = new Dictionary<WebExceptionStatus, int>();
        foreach (var v in Enum.GetValues(typeof(WebExceptionStatus)))
        {
            var status = (WebExceptionStatus)v!;
            var name = status.ToInvariantString();

            webStatuses[status] = descriptions.Count;
            descriptions.Add(name);
        }

        _descriptions = descriptions.ToImmutableArray();
        _socketErrorMap = socketErrors.ToFrozenDictionary();
        _webExceptionStatusMap = webStatuses.ToFrozenDictionary();
    }

    public IEnumerable<Type> SupportedExceptionTypes { get; } = new[]
    {
        typeof(TaskCanceledException),
        typeof(OperationCanceledException),
        typeof(WebException),
        typeof(SocketException),
    };

    public IReadOnlyList<string> Descriptions => _descriptions;

    public int Describe(Exception exception, out string? additionalDetails)
    {
        _ = Throw.IfNull(exception);

        additionalDetails = null;
        switch (exception)
        {
            case OperationCanceledException ex:
            {
                return ex.CancellationToken.IsCancellationRequested ? 0 : 1;
            }

            case WebException ex:
            {
                if (_webExceptionStatusMap.TryGetValue(ex.Status, out var index))
                {
                    return index;
                }

                break;
            }

            case SocketException ex:
            {
                if (_socketErrorMap.TryGetValue(ex.SocketErrorCode, out var index))
                {
                    return index;
                }

                break;
            }
        }

        return DefaultDescriptionIndex;
    }
}
