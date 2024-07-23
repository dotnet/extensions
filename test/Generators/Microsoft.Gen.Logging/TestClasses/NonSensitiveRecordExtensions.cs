// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;

public static partial class NonSensitiveRecordExtensions
{
    internal record User()
    {
        public int Id { get; set; }

        [NoDataClassification]
        public string? Email { get; set; }
    }

    // This is specifically to address https://github.com/dotnet/extensions/issues/5188
    internal static partial class Logging
    {
        [LoggerMessage(6001, LogLevel.Information, "Logging User: {User}")]
        public static partial void LogUser(ILogger logger, [LogProperties] User user);
    }
}
