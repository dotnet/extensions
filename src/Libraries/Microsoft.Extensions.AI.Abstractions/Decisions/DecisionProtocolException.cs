// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a decision response that does not correspond to its request.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionProtocolException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DecisionProtocolException"/> class.</summary>
    public DecisionProtocolException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionProtocolException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public DecisionProtocolException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionProtocolException"/> class with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public DecisionProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
