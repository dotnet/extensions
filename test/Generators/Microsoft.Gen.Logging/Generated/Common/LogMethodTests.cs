// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LogMethodTests
{
    [Fact]
    public void BasicTests()
    {
        var logger = new FakeLogger();

        NoNamespace.CouldNotOpenSocket(logger, "microsoft.com");
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("Could not open socket to `microsoft.com`", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        Level1.OneLevelNamespace.CouldNotOpenSocket(logger, "microsoft.com");
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("Could not open socket to `microsoft.com`", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        Level1.Level2.TwoLevelNamespace.CouldNotOpenSocket(logger, "microsoft.com");
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("Could not open socket to `microsoft.com`", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);
    }

#if ROSLYN_4_0_OR_GREATER
    [Fact]
    public void FileScopedNamespaceTest()
    {
        var logger = new FakeLogger();
        FileScopedNamespace.Log.CouldNotOpenSocket(logger, "microsoft.com");
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("Could not open socket to `microsoft.com`", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);
    }
#endif

    [Fact]
    public void EnableTest()
    {
        var logger = new FakeLogger();

        logger.ControlLevel(LogLevel.Trace, false);
        logger.ControlLevel(LogLevel.Debug, false);
        logger.ControlLevel(LogLevel.Information, false);
        logger.ControlLevel(LogLevel.Warning, false);
        logger.ControlLevel(LogLevel.Error, false);
        logger.ControlLevel(LogLevel.Critical, false);

        LevelTestExtensions.M8(logger, LogLevel.Trace);
        LevelTestExtensions.M8(logger, LogLevel.Debug);
        LevelTestExtensions.M8(logger, LogLevel.Information);
        LevelTestExtensions.M8(logger, LogLevel.Warning);
        LevelTestExtensions.M8(logger, LogLevel.Error);
        LevelTestExtensions.M8(logger, LogLevel.Critical);

        Assert.Equal(0, logger.Collector.Count);
    }

    [Fact]
    public void OptionalArgTest()
    {
        var logger = new FakeLogger();

        SignatureTestExtensions.M2(logger, "Hello");
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("Hello World", logger.LatestRecord.Message);
        Assert.Equal(3, logger.LatestRecord.StructuredState!.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState[0].Key);
        Assert.Equal("Hello", logger.LatestRecord.StructuredState[0].Value);
        Assert.Equal("p2", logger.LatestRecord.StructuredState[1].Key);
        Assert.Equal("World", logger.LatestRecord.StructuredState[1].Value);

        logger.Collector.Clear();
        SignatureTestExtensions.M2(logger, "Hello", "World");
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("Hello World", logger.LatestRecord.Message);
        Assert.Equal(3, logger.LatestRecord.StructuredState!.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState[0].Key);
        Assert.Equal("Hello", logger.LatestRecord.StructuredState[0].Value);
        Assert.Equal("p2", logger.LatestRecord.StructuredState[1].Key);
        Assert.Equal("World", logger.LatestRecord.StructuredState[1].Value);
    }

    [Fact]
    public void ArgTest()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        ArgTestExtensions.Method1(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M1", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method2(logger, "arg1");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M2 arg1", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method3(logger, "arg1", 2);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M3 arg1 2", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method4(logger, new InvalidOperationException("A"));
        Assert.Equal("A", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M4", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method5(logger, new InvalidOperationException("A"), new InvalidOperationException("B"));
        Assert.Equal("A", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M5 System.InvalidOperationException: B", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method6(logger, new InvalidOperationException("A"), 2);
        Assert.Equal("A", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M6 2", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method7(logger, 1, new InvalidOperationException("B"));
        Assert.Equal("B", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M7 1", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method8(logger, 1, 2, 3, 4, 5, 6, 7);
        Assert.Equal("M81234567", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method9(logger, 1, 2, 3, 4, 5, 6, 7);
        Assert.Equal("M9 1 2 3 4 5 6 7", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ArgTestExtensions.Method10(logger, 1);
        Assert.Equal("M101", logger.LatestRecord.Message);
        Assert.Equal(1, logger.Collector.Count);
    }

    [Fact]
    public void CollectionTest()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        CollectionTestExtensions.M0(logger);
        TestCollection(1, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M1(logger, 0);
        TestCollection(2, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M2(logger, 0, 1);
        TestCollection(3, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M3(logger, 0, 1, 2);
        TestCollection(4, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M4(logger, 0, 1, 2, 3);
        TestCollection(5, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M5(logger, 0, 1, 2, 3, 4);
        TestCollection(6, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M6(logger, 0, 1, 2, 3, 4, 5);
        TestCollection(7, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M7(logger, 0, 1, 2, 3, 4, 5, 6);
        TestCollection(8, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M8(logger, 0, 1, 2, 3, 4, 5, 6, 7);
        TestCollection(9, logger);

        logger.Collector.Clear();
        CollectionTestExtensions.M9(logger, LogLevel.Critical, 0, new ArgumentException("Foo"), 1);
        TestCollection(3, logger);
    }

    [Fact]
    public void ConstructorVariationsTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M0(logger, "Zero");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0 Zero", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(0, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M1(logger, LogLevel.Trace, "One");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M1 One", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M2(logger, "Two");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(2, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M3(logger, LogLevel.Trace, "Three");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(3, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M4(logger, "Four");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M4 Four", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(0, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M5(logger, LogLevel.Trace, "Five");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M5 Five", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(0, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M6(logger, "Six");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(0, logger.LatestRecord.Id.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ConstructorVariationsTestExtensions.M7(logger, LogLevel.Information, "Seven");
        Assert.Equal(1, logger.Collector.Count);

        var logRecord = logger.LatestRecord;
        Assert.Null(logRecord.Exception);
        Assert.Equal(string.Empty, logRecord.Message);
        Assert.Equal(LogLevel.Information, logRecord.Level);
        Assert.Equal(0, logRecord.Id.Id);
        Assert.Equal("M7", logRecord.Id.Name);
    }

    [Fact]
    public void MessageTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        MessageTestExtensions.M0(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MessageTestExtensions.M1(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MessageTestExtensions.M2(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MessageTestExtensions.M5(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("\"Hello\" World", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MessageTestExtensions.M6(logger, LogLevel.Trace, "p", "q");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("\"p\" -> \"q\"", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(6, logger.LatestRecord.Id);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MessageTestExtensions.M7(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("\"\n\r\\", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(7, logger.LatestRecord.Id);
        Assert.Equal(1, logger.Collector.Count);
    }

    [Fact]
    public void InstanceTests()
    {
        var logger = new FakeLogger();
        var o = new TestInstances(logger);

        logger.Collector.Clear();
        o.M0();
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        o.M1("Foo");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M1 Foo", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        o.M2(LogLevel.Warning, "param");
        Assert.Equal(1, logger.Collector.Count);

        var logRecord = logger.LatestRecord;
        Assert.Null(logRecord.Exception);
        Assert.Equal(string.Empty, logRecord.Message);
        Assert.Equal(LogLevel.Warning, logRecord.Level);
        Assert.NotNull(logRecord.StructuredState);
        Assert.Single(logRecord.StructuredState!);
        Assert.Equal("p1", logRecord.StructuredState![0].Key);
        Assert.Equal("param", logRecord.StructuredState[0].Value);
    }

    [Fact]
    public void LevelTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        LevelTestExtensions.M0(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M1(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M1", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M2(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M2", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M3(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M3", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M4(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M4", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M5(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M5", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M6(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M6", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.None, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M7(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M7", logger.LatestRecord.Message);
        Assert.Equal((LogLevel)42, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M8(logger, LogLevel.Critical);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M8", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Critical, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M9(LogLevel.Trace, logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M9", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M10(logger, LogLevel.Trace);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M10 Trace", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        LevelTestExtensions.M11(logger, LogLevel.Trace);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M11 Microsoft.Extensions.Telemetry.Testing.Logging.FakeLogger", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);
    }

    [Fact]
    public void LevelTests_ForNonStatic()
    {
        var logger = new FakeLogger();

        var redactorProvider = new StarRedactorProvider();
        var instance = new NonStaticTestClass(logger, redactorProvider);

        instance.NoParams();
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("No params here...", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        instance.NoParamsWithLevel(LogLevel.Information);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("No params here as well...", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Information, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        instance.NoParamsWithLevel(LogLevel.Error);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("No params here as well...", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);
    }

    [Fact]
    public void NonStaticNullable()
    {
        var logger = new FakeLogger();
        var redactorProvider = new StarRedactorProvider();

        var instance = new NonStaticNullableTestClass(logger, redactorProvider);
        instance.M2("One", "Two", "Three");
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M2 *** *** *****", logger.LatestRecord.Message);

        logger.Collector.Clear();
        instance = new NonStaticNullableTestClass(null, redactorProvider);
        instance.M2("One", "Two", "Three");
        Assert.Equal(0, logger.Collector.Count);

        logger.Collector.Clear();
        instance = new NonStaticNullableTestClass(logger, null);
        instance.M2("One", "Two", "Three");
        Assert.Equal("M2 (null) (null) (null)", logger.LatestRecord.Message);
    }

    [Fact]
    public void ExceptionTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        ExceptionTestExtensions.M0(logger, new ArgumentException("Foo"), new ArgumentException("Bar"));
        Assert.Equal("Foo", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M0 System.ArgumentException: Bar", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ExceptionTestExtensions.M1(new ArgumentException("Foo"), logger, new ArgumentException("Bar"));
        Assert.Equal("Foo", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M1 System.ArgumentException: Bar", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        ExceptionTestExtensions.M2(logger, "One", new ArgumentException("Foo"));
        Assert.Equal("Foo", logger.LatestRecord.Exception!.Message);
        Assert.Equal("M2 One: System.ArgumentException: Foo", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        var exception = new ArgumentException("Foo");
        ExceptionTestExtensions.M3(exception, logger, LogLevel.Error);
        Assert.Equal(1, logger.Collector.Count);
        Assert.NotNull(logger.LatestRecord.Exception);
        Assert.Equal(exception.Message, logger.LatestRecord.Exception!.Message);
        Assert.Equal(exception.GetType(), logger.LatestRecord.Exception!.GetType());
        Assert.Equal(string.Empty, logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Error, logger.LatestRecord.Level);
    }

    [Fact]
    public void EventNameTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        EventNameTestExtensions.M0(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Trace, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("CustomEventName", logger.LatestRecord.Id.Name);

        logger.Collector.Clear();
        EventNameTestExtensions.M1(LogLevel.Trace, logger, "Eight");
        Assert.Equal(1, logger.Collector.Count);

        var logRecord = logger.LatestRecord;
        Assert.Null(logRecord.Exception);
        Assert.Equal(string.Empty, logRecord.Message);
        Assert.Equal(LogLevel.Trace, logRecord.Level);
        Assert.Equal(0, logRecord.Id.Id);
        Assert.Equal("M1_Event", logRecord.Id.Name);
    }

    [Fact]
    public void NestedClassTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        NestedClassTestExtensions<Alien.Abc>.NestedMiddleParentClass.NestedClass.M8(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M8", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        NonStaticNestedClassTestExtensions<Alien.Abc>.NonStaticNestedMiddleParentClass.NestedClass.M9(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M9", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        NestedStruct.Logger.M10(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M10", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        NestedRecord.Logger.M11(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M11", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);

        logger.Collector.Clear();
        MultiLevelNestedClass.NestedStruct.NestedRecord.Logger.M12(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M12", logger.LatestRecord.Message);
        Assert.Equal(LogLevel.Debug, logger.LatestRecord.Level);
        Assert.Equal(1, logger.Collector.Count);
    }

    [Fact]
    public void TemplateTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        TemplateTestExtensions.M0(logger, 0);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0 0", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("A1", "0"),
            new KeyValuePair<string, string?>("{OriginalFormat}", "M0 {A1}"));

        logger.Collector.Clear();
        TemplateTestExtensions.M1(logger, 42);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M1 42 42", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("A1", "42"),
            new KeyValuePair<string, string?>("{OriginalFormat}", "M1 {A1} {A1}"));

        logger.Collector.Clear();
        TemplateTestExtensions.M2(logger, 42, 43, 44, 45, 46, 47, 48);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M2 42 43 44 45 46 47 48", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("A1", "42"),
            new KeyValuePair<string, string?>("a2", "43"),
            new KeyValuePair<string, string?>("A3", "44"),
            new KeyValuePair<string, string?>("a4", "45"),
            new KeyValuePair<string, string?>("A5", "46"),
            new KeyValuePair<string, string?>("a6", "47"),
            new KeyValuePair<string, string?>("A7", "48"),
            new KeyValuePair<string, string?>("{OriginalFormat}", "M2 {A1} {a2} {A3} {a4} {A5} {a6} {A7}"));

        logger.Collector.Clear();
        TemplateTestExtensions.M3(logger, 42, 43);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M3 43 42", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("A1", "42"),
            new KeyValuePair<string, string?>("a2", "43"),
            new KeyValuePair<string, string?>("{OriginalFormat}", "M3 {a2} {A1}"));
    }

    [Fact]
    public void StructTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        StructTestExtensions.M0(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("{OriginalFormat}", "M0"));
    }

    [Fact]
    public void RecordTests()
    {
        var logger = new FakeLogger();

        logger.Collector.Clear();
        RecordTestExtensions.M0(logger);
        Assert.Null(logger.LatestRecord.Exception);
        Assert.Equal("M0", logger.LatestRecord.Message);
        AssertLastState(logger,
            new KeyValuePair<string, string?>("{OriginalFormat}", "M0"));
    }

    [Fact]
    public void SkipEnabledCheckTests()
    {
        var logger = new FakeLogger();
        logger.ControlLevel(LogLevel.Information, false);

        SkipEnabledCheckTestExtensions.LoggerMethodWithFalseSkipEnabledCheck(logger);
        Assert.Equal(0, logger.Collector.Count);

        SkipEnabledCheckTestExtensions.LoggerMethodWithFalseSkipEnabledCheck(logger, LogLevel.Information, "p1");
        Assert.Equal(0, logger.Collector.Count);

#if NET6_0_OR_GREATER
        SkipEnabledCheckTestExtensions.LoggerMethodWithTrueSkipEnabledCheck(logger);
        Assert.Equal(1, logger.Collector.Count);
#endif
    }

    [Fact]
    public void InParameterTests()
    {
        var logger = new FakeLogger();

        InParameterTestExtensions.S s;
        InParameterTestExtensions.M0(logger, in s);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Contains("Hello from S", logger.Collector.LatestRecord.Message);
    }

    [Fact]
    public void AtSymbolsTest()
    {
        var logger = new FakeLogger();

        AtSymbolsTestExtensions.M0(logger, "Test");
        var record = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal("M0 Test", record.Message);
        Assert.NotNull(record.StructuredState);
        Assert.Contains(record.StructuredState, x => x.Key == "event");
        Assert.DoesNotContain(record.StructuredState, x => x.Key == "@event");

        AtSymbolsTestExtensions.M1(logger, new StarRedactorProvider(), "Test");
        Assert.Equal(2, logger.Collector.Count);
    }

    [Fact]
    public void OverloadsTest()
    {
        var logger = new FakeLogger();

        OverloadsTestExtensions.M0(logger, "Test");
        OverloadsTestExtensions.M0(logger, 42);
        Assert.Equal(2, logger.Collector.Count);
    }

    [Fact]
    public void NullableTests()
    {
        var logger = new FakeLogger();
        var redactorProvider = new StarRedactorProvider();

        NullableTestExtensions.M0(logger, null);
        Assert.Equal("M0 (null)", logger.LatestRecord.Message);

        NullableTestExtensions.M1(logger, null);
        Assert.Equal("M1 (null)", logger.LatestRecord.Message);

        NullableTestExtensions.M3(logger, redactorProvider, null);
        Assert.Equal("M3 (null)", logger.LatestRecord.Message);

        NullableTestExtensions.M4(logger, null, null, null, null, null, null, null, null, null);
        Assert.Equal("M4 (null) (null) (null) (null) (null) (null) (null) (null) (null)", logger.LatestRecord.Message);

        NullableTestExtensions.M5(logger, null, null, null, null, null, null, null, null, null);
        Assert.Equal("M5 (null) (null) (null) (null) (null) (null) (null) (null) (null)", logger.LatestRecord.Message);

        logger.Collector.Clear();
        NullableTestExtensions.M6(null, null, "Nothing");
        Assert.Equal(0, logger.Collector.Count);

        NullableTestExtensions.M6(logger, null, "Nothing");
        Assert.Equal("M6 (null)", logger.LatestRecord.Message);
    }

    [Fact]
    public void InvariantTest()
    {
        var logger = new FakeLogger();
        var dt = new DateTime(2022, 5, 22);

        var oldCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-CA");

        InvariantTestExtensions.M0(logger, dt);
        Assert.Equal(dt.ToString(CultureInfo.InvariantCulture), logger.LatestRecord.StructuredState![0].Value);
        Assert.Equal("M0 " + dt.ToString(CultureInfo.InvariantCulture), logger.LatestRecord.Message);

        Thread.CurrentThread.CurrentCulture = oldCulture;
    }

    [Fact]
    public void EnumerableTest()
    {
        var logger = new FakeLogger();

        EnumerableTestExtensions.M10(logger,
            new[] { 1, 2, 3 },
            new[] { 4, 5, 6 },
            new Dictionary<string, int>
            {
                { "Seven", 7 },
                { "Eight", 8 },
                { "Nine", 9 }
            });

        Assert.Equal(1, logger.Collector.Count);

        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", logger.LatestRecord.StructuredState[0].Value);

        Assert.Equal("p2", logger.LatestRecord.StructuredState[1].Key);
        Assert.Equal("[\"4\",\"5\",\"6\"]", logger.LatestRecord.StructuredState[1].Value);

        Assert.Equal("p3", logger.LatestRecord.StructuredState[2].Key);
        Assert.Equal("{\"Seven\"=\"7\",\"Eight\"=\"8\",\"Nine\"=\"9\"}", logger.LatestRecord.StructuredState[2].Value);
    }

    [Fact]
    public void NullableEnumerableTest()
    {
        var logger = new FakeLogger();

        EnumerableTestExtensions.M11(logger, null);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Null(logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        EnumerableTestExtensions.M11(logger, new[] { 1, 2, 3 });
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        EnumerableTestExtensions.M12(logger, null);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("class", logger.LatestRecord.StructuredState![0].Key);
        Assert.Null(logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        EnumerableTestExtensions.M12(logger, new[] { 1, 2, 3 });
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("class", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        EnumerableTestExtensions.M13(logger, default);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
#pragma warning disable SA1129 // Do not use default value type constructor
        EnumerableTestExtensions.M14(logger, new StructEnumerable());
#pragma warning restore SA1129 // Do not use default value type constructor
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("[\"1\",\"2\",\"3\"]", logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        EnumerableTestExtensions.M14(logger, default);
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Null(logger.LatestRecord.StructuredState[0].Value);
    }

    [Fact]
    public void FormattableTest()
    {
        var logger = new FakeLogger();
        FormattableTestExtensions.Method1(logger, new FormattableTestExtensions.Formattable());
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1", logger.LatestRecord.StructuredState![0].Key);
        Assert.Equal("Formatted!", logger.LatestRecord.StructuredState[0].Value);

        logger.Collector.Clear();
        FormattableTestExtensions.Method2(logger, new FormattableTestExtensions.ComplexObj());
        Assert.Equal(1, logger.Collector.Count);
        Assert.Equal("p1_P1", logger.LatestRecord.StructuredState![1].Key);
        Assert.Equal("Formatted!", logger.LatestRecord.StructuredState[1].Value);
    }

    private static void AssertLastState(FakeLogger logger, params KeyValuePair<string, string?>[] expected)
    {
        var rol = (IReadOnlyList<KeyValuePair<string, string?>>)logger.LatestRecord.State!;
        int count = 0;
        foreach (var kvp in expected)
        {
            Assert.Equal(kvp.Key, rol[count].Key);
            Assert.Equal(kvp.Value, rol[count].Value);
            count++;
        }
    }

    [SuppressMessage("Minor Code Smell", "S4056:Overloads with a \"CultureInfo\" or an \"IFormatProvider\" parameter should be used", Justification = "Not appropriate here")]
    private static void TestCollection(int expected, FakeLogger logger)
    {
        var rol = (logger.LatestRecord.State as IReadOnlyList<KeyValuePair<string, string?>>)!;
        Assert.NotNull(rol);

        Assert.Equal(expected, rol.Count);
        for (int i = 0; i < expected; i++)
        {
            if (i != expected - 1)
            {
                var kvp = new KeyValuePair<string, string?>($"p{i}", i.ToString());
                Assert.Equal(kvp, rol[i]);
            }
        }

        int count = 0;
        foreach (var actual in rol)
        {
            if (count != expected - 1)
            {
                var kvp = new KeyValuePair<string, string?>($"p{count}", count.ToString());
                Assert.Equal(kvp, actual);
            }

            count++;
        }

        Assert.Equal(expected, count);
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = rol[expected]);
    }
}
