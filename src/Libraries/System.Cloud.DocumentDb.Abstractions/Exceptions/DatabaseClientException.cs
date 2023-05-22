// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Exception represent the operation is failed w/ a specific reason and should not retry.
/// </summary>
/// <remarks>
/// Please check the log and eliminate this kind of requests.
/// Http code 400, 401, 403, 413.
/// Covered codes may vary on specific engine requirements.
/// </remarks>
public class DatabaseClientException : DatabaseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseClientException"/> class.
    /// </summary>
    public DatabaseClientException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseClientException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public DatabaseClientException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseClientException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception related to the missing data.</param>
    public DatabaseClientException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
