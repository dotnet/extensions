// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
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
        OriginalWithSampling,
        New,
        NewWithSampling,
        NewWithEnrichers,
        NewWithEnrichersAndSampling
    }

    private readonly ILogger[] _loggers = new[]
    {
        GetLogger(LoggerFactoryVersions.Original),
        GetLogger(LoggerFactoryVersions.OriginalWithSampling),
        GetLogger(LoggerFactoryVersions.New),
        GetLogger(LoggerFactoryVersions.NewWithSampling),
        GetLogger(LoggerFactoryVersions.NewWithEnrichers),
        GetLogger(LoggerFactoryVersions.NewWithEnrichersAndSampling)
    };

    [Params(
        LoggerFactoryVersions.Original,
        LoggerFactoryVersions.OriginalWithSampling,
        LoggerFactoryVersions.New,
        LoggerFactoryVersions.NewWithSampling,
        LoggerFactoryVersions.NewWithEnrichers,
        LoggerFactoryVersions.NewWithEnrichersAndSampling)]
    public LoggerFactoryVersions Factory;

    private static ILogger GetLogger(LoggerFactoryVersions config)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(builder =>
        {
            builder.AddProvider(new BenchLoggerProvider());

            switch (config)
            {
                case LoggerFactoryVersions.OriginalWithSampling:
                    builder.AddRandomProbabilisticSampler(1.0);
                    break;
                case LoggerFactoryVersions.New:
                case LoggerFactoryVersions.NewWithEnrichers:
                    builder.EnableEnrichment();
                    builder.EnableRedaction();
                    break;
                case LoggerFactoryVersions.NewWithSampling:
                case LoggerFactoryVersions.NewWithEnrichersAndSampling:
                    builder.EnableEnrichment();
                    builder.EnableRedaction();
                    builder.AddRandomProbabilisticSampler(1.0);
                    break;
            }
        });

        if (config is LoggerFactoryVersions.NewWithEnrichers or LoggerFactoryVersions.NewWithEnrichersAndSampling)
        {
            serviceCollection.AddProcessLogEnricher();
            serviceCollection.AddRedaction(builder => builder.SetFallbackRedactor<ErasingRedactor>());
        }

        return serviceCollection.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger("Benchmark");
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        Classic_RefTypes();
        Classic_ValueTypes();
        LoggerMessageDefine_RefTypes();
        LoggerMessageDefine_ValueTypes();
        ClassicCodeGen_RefTypes();
        ClassicCodeGen_ValueTypes();
        ModernCodeGen_RefTypes();
        ModernCodeGen_ValueTypes();
    }

    [Benchmark]
    [SuppressMessage("Performance", "EA0000:Use source generated logging methods for improved performance", Justification = "Benchmark")]
    public void Classic_RefTypes()
    {
        ILogger logger = _loggers[(int)Factory];

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
        ILogger logger = _loggers[(int)Factory];

        logger.LogError(@"Range [{start}..{end}], options {options}, guid {guid}",
            Start,
            End,
            Options,
            _guid);
    }

    [Benchmark]
    public void LoggerMessageDefine_RefTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        _loggerMessage_refTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other, null);
    }

    [Benchmark]
    public void LoggerMessageDefine_ValueTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        _loggerMessage_valueTypes(logger, Start, End, Options, _guid, null);
    }

    [Benchmark]
    public void ClassicCodeGen_RefTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        ClassicCodeGen.RefTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void ClassicCodeGen_ValueTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        ClassicCodeGen.ValueTypes(logger, Start, End, Options, _guid);
    }

    [Benchmark]
    public void ModernCodeGen_RefTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        ModernCodeGen.RefTypes(logger, ConnectionId, Type, StreamId, Length, Flags, Other);
    }

    [Benchmark]
    public void ModernCodeGen_ValueTypes()
    {
        ILogger logger = _loggers[(int)Factory];
        ModernCodeGen.ValueTypes(logger, Start, End, Options, _guid);
    }
}
