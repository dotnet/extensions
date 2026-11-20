// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a provider failure for a whole decision request, with a transient classification.</summary>
[Experimental(DiagnosticIds.Experiments.AIDecisions, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class DecisionClientException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DecisionClientException"/> class.</summary>
    public DecisionClientException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionClientException"/> class with a message.</summary>
    /// <param name="message">The error message.</param>
    public DecisionClientException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionClientException"/> class with a message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public DecisionClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DecisionClientException"/> class with a transient classification.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="isTransient">Whether the provider classified the failure as transient.</param>
    /// <param name="innerException">The underlying exception, if any.</param>
    public DecisionClientException(string message, bool isTransient, Exception? innerException = null)
        : base(message, innerException)
    {
        IsTransient = isTransient;
    }

    /// <summary>Gets a value indicating whether the provider classified the failure as transient.</summary>
    public bool IsTransient { get; }
}
