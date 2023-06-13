// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The exception that's thrown when the operation failed without a specific reason.
/// </summary>
/// <remarks>
/// It might due to some failures on server side.
/// Ask the engineer to investigate this case and escalate if necessary.
/// Http code 500.
/// </remarks>
public class DatabaseServerException : DatabaseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    public DatabaseServerException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseServerException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    public DatabaseServerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request info.</param>
    public DatabaseServerException(string message, Exception innerException, int statusCode, int subStatusCode, RequestInfo requestInfo)
        : base(message, innerException, statusCode, subStatusCode, requestInfo)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseServerException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="requestInfo">The request info.</param>
    public DatabaseServerException(string message, int statusCode, int subStatusCode, RequestInfo requestInfo)
        : base(message, statusCode, subStatusCode, requestInfo)
    {
    }
}
