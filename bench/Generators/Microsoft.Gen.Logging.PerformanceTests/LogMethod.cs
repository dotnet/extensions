// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable R9A000 // Switch to updated logging methods using the [LogMethod] attribute for additional performance.

namespace Microsoft.Gen.Logging.Bench;

[MemoryDiagnoser]
public class LogMethod
{
    private const string ConnectionId = "0x345334534678";
    private const string Type = "some string";
    private const string StreamId = "some string some string";
    private const string Length = "some string some string some string";
    private const string Flags = "some string some string some string some string";
    private const string Other = "some string some string some string some string some string";
    private const long Start = 42;
    private const long End = 123_456_789;
    private const int Options = 0x1234;

    private static readonly Guid _guid = new(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 });

    private static readonly Action<ILogger, string, string, string, string, string, string, Exception?> _loggerMessage_refTypes = LoggerMessage.Define<string, string, string, string, string, string>(
        LogLevel.Error,
        eventId: 380,
        formatString: @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}");

    private static readonly Action<ILogger, long, long, int, Guid, Exception?> _loggerMessage_valueTypes = LoggerMessage.Define<long, long, int, Guid>(
        LogLevel.Error,
        eventId: 381,
        formatString: @"Range [{start}..{end}], options {options}, guid {guid}");

    private static readonly MockLogger _logger = new();

    [Params(true, false)]
    public bool Enabled;

    [Benchmark]
    public void Classic_RefTypes()
    {
        _logger.Enabled = Enabled;
        _logger.LogError(
            @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}",
            ConnectionId,
            Type,
            StreamId,
            Length,
            Flags,
            Other);
    }

    [Benchmark]
    public void Classic_ValueTypes()
    {
        _logger.Enabled = Enabled;
        _logger.LogError(@"Range [{start}..{end}], options {options}, guid {guid}",
            Start,
            End,
            Options,
            _guid);
    }

    [Benchmark]
    public void LoggerMessage_RefTypes()
    {
        _logger.Enabled = Enabled;
        _loggerMessage_refTypes(_logger, ConnectionId, Type, StreamId, Length, Flags, Other, null);
    }

    [Benchmark]
    public void LoggerMessage_ValueTypes()
    {
        _logger.Enabled = Enabled;
        _loggerMessage_valueTypes(_logger, Start, End, Options, _guid, null);
    }

    [Benchmark]
    public void LogMethod_RefTypes_Error()
    {
        _logger.Enabled = Enabled;
        Log.RefTypes_Error(_logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void LogMethod_RefTypes_Debug()
    {
        _logger.Enabled = Enabled;
        Log.RefTypes_Debug(_logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void LogMethod_ValueTypes_Error()
    {
        _logger.Enabled = Enabled;
        Log.ValueTypes_Error(_logger, Start, End, Options, _guid);
    }

    [Benchmark]
    public void LogMethod_ValueTypes_Debug()
    {
        _logger.Enabled = Enabled;
        Log.ValueTypes_Debug(_logger, Start, End, Options, _guid);
    }
}
