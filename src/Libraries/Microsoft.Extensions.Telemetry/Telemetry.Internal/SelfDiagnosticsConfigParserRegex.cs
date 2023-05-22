// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Telemetry.Internal;

#if NET7_0_OR_GREATER
internal static partial class SelfDiagnosticsConfigParserRegex
#else
internal static class SelfDiagnosticsConfigParserRegex
#endif
{
    private const string LogDirectoryRegexString = @"""LogDirectory""\s*:\s*""(?<LogDirectory>.*?)""";
    private const string FileSizeRegexString = @"""FileSize""\s*:\s*(?<FileSize>\d+)";
    private const string LogLevelRegexString = @"""LogLevel""\s*:\s*""(?<LogLevel>.*?)""";

#if NET7_0_OR_GREATER

    [GeneratedRegex(LogDirectoryRegexString, RegexOptions.IgnoreCase)]
    public static partial Regex MakeLogDirectoryRegex();

    [GeneratedRegex(FileSizeRegexString, RegexOptions.IgnoreCase)]
    public static partial Regex MakeFileSizeRegex();

    [GeneratedRegex(LogLevelRegexString, RegexOptions.IgnoreCase)]
    public static partial Regex MakeLogLevelRegex();

#else

    public static Regex MakeLogDirectoryRegex() => new(LogDirectoryRegexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static Regex MakeFileSizeRegex() => new(FileSizeRegexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    public static Regex MakeLogLevelRegex() => new(LogLevelRegexString, RegexOptions.IgnoreCase | RegexOptions.Compiled);

#endif
}
