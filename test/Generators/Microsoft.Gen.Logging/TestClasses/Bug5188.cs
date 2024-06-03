using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Logging;

public static partial class Bug5188
{
    internal record User()
    {
        public int Id { get; set; }

        [NoDataClassification]
        public string? Email { get; set; }
    }

    internal static partial class Logging
    {
        [LoggerMessage(6001, LogLevel.Information, "Logging User: {User}")]
        public static partial void LogUser(ILogger logger, [LogProperties] User user);
    }
}
