// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Exception represent the operation is failed w/ a specific reason and it's eligible to retry in future.
/// </summary>
/// <remarks>
/// Http code 429, 503, 408.
/// Covered codes may vary on specific engine requirements.
/// </remarks>
public class DatabaseRetryableException : DatabaseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRetryableException"/> class.
    /// </summary>
    public DatabaseRetryableException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRetryableException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseRetryableException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRetryableException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    public DatabaseRetryableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseRetryableException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">Exception related to the missing data.</param>
    /// <param name="statusCode">Exception status code.</param>
    /// <param name="subStatusCode">Exception sub status code.</param>
    /// <param name="retryAfter">Retry after timespan.</param>
    /// <param name="requestInfo">The request.</param>
    public DatabaseRetryableException(
        string message,
        Exception innerException,
        int statusCode,
        int subStatusCode,
        TimeSpan? retryAfter,
        RequestInfo requestInfo)
        : base(message, innerException, statusCode, subStatusCode, requestInfo)
    {
        if (retryAfter.HasValue)
        {
            RetryAfter = retryAfter.Value;
        }
    }

    /// <summary>
    /// Gets a value indicate the retry after time.
    /// </summary>
    public TimeSpan RetryAfter { get; } = TimeSpan.Zero;
}
