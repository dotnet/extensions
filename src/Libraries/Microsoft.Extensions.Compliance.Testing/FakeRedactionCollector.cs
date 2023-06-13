// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Usage history of fake redaction types.
/// </summary>
public class FakeRedactionCollector
{
    private readonly List<RedactorRequested> _redactorRequestedLog = new();
    private readonly List<RedactedData> _dataRedactedLog = new();

    /// <summary>
    /// Gets the last redactor request "event".
    /// </summary>
    /// <exception cref="InvalidOperationException">There has been no previous redactor request event.</exception>
    public RedactorRequested LastRedactorRequested
    {
        get
        {
            lock (_redactorRequestedLog)
            {
                if (_redactorRequestedLog.Count == 0)
                {
                    Throw.InvalidOperationException("No redactor requested.");
                }

                return _redactorRequestedLog[_redactorRequestedLog.Count - 1];
            }
        }
    }

    /// <summary>
    /// Gets the full log of all redactor request events that happened.
    /// </summary>
    public IReadOnlyList<RedactorRequested> AllRedactorRequests
    {
        get
        {
            lock (_redactorRequestedLog)
            {
                return _redactorRequestedLog.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets the last redaction "event".
    /// </summary>
    /// <exception cref="InvalidOperationException">There has been no previous redaction event.</exception>
    public RedactedData LastRedactedData
    {
        get
        {
            lock (_dataRedactedLog)
            {
                if (_dataRedactedLog.Count == 0)
                {
                    Throw.InvalidOperationException("No data redacted.");
                }

                return _dataRedactedLog[_dataRedactedLog.Count - 1];
            }
        }
    }

    /// <summary>
    /// Gets the full log of all redaction events that happened.
    /// </summary>
    public IReadOnlyList<RedactedData> AllRedactedData
    {
        get
        {
            lock (_dataRedactedLog)
            {
                return _dataRedactedLog.ToArray();
            }
        }
    }

    internal void Append(RedactedData redactedData)
    {
        lock (_dataRedactedLog)
        {
            _dataRedactedLog.Add(redactedData);
        }
    }

    internal void Append(RedactorRequested redactorRequested)
    {
        lock (_redactorRequestedLog)
        {
            _redactorRequestedLog.Add(redactorRequested);
        }
    }
}
