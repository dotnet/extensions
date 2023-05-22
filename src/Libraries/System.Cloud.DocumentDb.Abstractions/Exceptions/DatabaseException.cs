// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Base type for exceptions thrown by storage adapter.
/// </summary>
public class DatabaseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    public DatabaseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception causing this exception.</param>
    public DatabaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseException(string message, int statusCode, int subStatusCode, RequestInfo requestInfo)
        : base(message)
    {
        StatusCode = statusCode;
        SubStatusCode = subStatusCode;
        RequestInfo = requestInfo;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception causing this exception.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseException(string message, Exception innerException, int statusCode, int subStatusCode, RequestInfo requestInfo)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        SubStatusCode = subStatusCode;
        RequestInfo = requestInfo;
    }

    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public int StatusCode { get; } = (int)HttpStatusCode.InternalServerError;

    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public HttpStatusCode HttpStatusCode => (HttpStatusCode)StatusCode;

    /// <summary>
    /// Gets the status code indicating the exception root cause.
    /// </summary>
    public int SubStatusCode { get; }

    /// <summary>
    /// Gets the request information.
    /// </summary>
    public RequestInfo RequestInfo { get; }
}
