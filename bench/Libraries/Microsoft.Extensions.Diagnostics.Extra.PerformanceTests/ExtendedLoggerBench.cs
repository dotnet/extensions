// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

[MemoryDiagnoser]
public class ExtendedLoggerBench
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

    public enum LoggerFactoryVersions
    {
        Original,
        New,
        NewWithEnrichers
    }

    private readonly ILogger[] _loggers = new[]
    {
        GetLogger(LoggerFactoryVersions.Original),
        GetLogger(LoggerFactoryVersions.New),
        GetLogger(LoggerFactoryVersions.NewWithEnrichers),
    };

    [Params(LoggerFactoryVersions.Original, LoggerFactoryVersions.New, LoggerFactoryVersions.NewWithEnrichers)]
    public LoggerFactoryVersions Factory;

    private static ILogger GetLogger(LoggerFactoryVersions config)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());

            if (config == LoggerFactoryVersions.New || config == LoggerFactoryVersions.NewWithEnrichers)
            {
                builder.EnableEnrichment();
                builder.EnableRedaction();
            }
        });

        if (config == LoggerFactoryVersions.NewWithEnrichers)
        {
            serviceCollection.AddProcessLogEnricher();
            serviceCollection.AddRedaction(builder => builder.SetFallbackRedactor<ErasingRedactor>());
        }

        return serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Benchmark");
    }

    [Benchmark]
    [SuppressMessage("Performance", "EA0000:Use source generated logging methods for improved performance", Justification = "Benchmark")]
    public void Classic_RefTypes()
    {
        var logger = _loggers[(int)Factory];

        logger.LogError(
            @"Connection id '{connectionId}' received {type} frame for stream ID {streamId} with length {length} and flags {flags} and {other}",
            ConnectionId,
            Type,
            StreamId,
            Length,
            Flags,
            Other);
    }

    [Benchmark]
    [SuppressMessage("Performance", "EA0000:Use source generated logging methods for improved performance", Justification = "Benchmark")]
    public void Classic_ValueTypes()
    {
        var logger = _loggers[(int)Factory];

        logger.LogError(@"Range [{start}..{end}], options {options}, guid {guid}",
            Start,
            End,
            Options,
            _guid);
    }

    [Benchmark]
    public void LoggerMessageDefine_RefTypes()
    {
        var logger = _loggers[(int)Factory];
        _loggerMessage_refTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other, null);
    }

    [Benchmark]
    public void LoggerMessageDefine_ValueTypes()
    {
        var logger = _loggers[(int)Factory];
        _loggerMessage_valueTypes(logger, Start, End, Options, _guid, null);
    }

    [Benchmark]
    public void ClassicCodeGen_RefTypes()
    {
        var logger = _loggers[(int)Factory];
        ClassicCodeGen.RefTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void ClassicCodeGen_ValueTypes()
    {
        var logger = _loggers[(int)Factory];
        ClassicCodeGen.ValueTypes(logger, Start, End, Options, _guid);
    }

    [Benchmark]
    public void ModernCodeGen_RefTypes()
    {
        var logger = _loggers[(int)Factory];
        ModernCodeGen.RefTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void ModernCodeGen_ValueTypes()
    {
        var logger = _loggers[(int)Factory];
        ModernCodeGen.ValueTypes(logger, Start, End, Options, _guid);
    }
}
