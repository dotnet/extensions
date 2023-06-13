// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// An exception class that should only be used for fault injection purposes.
/// </summary>
public class InjectedFaultException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectedFaultException"/> class.
    /// </summary>
    public InjectedFaultException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectedFaultException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InjectedFaultException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InjectedFaultException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception,
    /// or a <see langword="null"/> reference (Nothing in Visual Basic) if no inner exception is specified.
    /// </param>
    public InjectedFaultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
