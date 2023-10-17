// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class ExtendedLoggerFactoryTests
{
    [Fact]
    public static void AddProvider_ThrowsAfterDisposed()
    {
        var factory = Utils.CreateLoggerFactory();
        factory.Dispose();

        Assert.Throws<ObjectDisposedException>(() => factory.AddProvider(CreateProvider()));
    }

    [Fact]
    public static void AddProvider_ThrowsIfProviderIsNull()
    {
        using var factory = Utils.CreateLoggerFactory();

        Assert.Throws<ArgumentNullException>(() => factory.AddProvider(null!));
    }

    [Fact]
    public static void CreateLogger_ThrowsAfterDisposed()
    {
        var factory = Utils.CreateLoggerFactory();
        factory.Dispose();
        Assert.Throws<ObjectDisposedException>(() => factory.CreateLogger("d"));
    }

    [Fact]
    public static void Dispose_MultipleCallsNoop()
    {
        var factory = Utils.CreateLoggerFactory();
        factory.Dispose();
#pragma warning disable S3966 // Objects should not be disposed more than once
        factory.Dispose();
#pragma warning restore S3966 // Objects should not be disposed more than once

        // just trying to make sure we didn't crash
        Assert.True(true);
    }

    [Fact]
    public static void Dispose_ProvidersAreDisposed()
    {
        // Arrange
        var factory = Utils.CreateLoggerFactory();
        var disposableProvider1 = CreateProvider();
        var disposableProvider2 = CreateProvider();

        factory.AddProvider(disposableProvider1);
        factory.AddProvider(disposableProvider2);

        // Act
        factory.Dispose();

        // Assert
        Mock.Get<IDisposable>(disposableProvider1)
                .Verify(p => p.Dispose(), Times.Once());
        Mock.Get<IDisposable>(disposableProvider2)
                 .Verify(p => p.Dispose(), Times.Once());
    }

    [Fact]
    public static void AddProvider_Updates_Loggers()
    {
        using var factory = Utils.CreateLoggerFactory();
        using var provider1 = new Provider();
        using var provider2 = new Provider();

        factory.AddProvider(provider1);
        var logger1 = (ExtendedLogger)factory.CreateLogger("C1");

        factory.AddProvider(provider2);
        var logger2 = (ExtendedLogger)factory.CreateLogger("C2");

        logger1.LogWarning("MSG0");
        logger2.LogWarning("MSG1");

        Assert.Equal(2, logger1.MessageLoggers.Length);
        Assert.Equal(2, logger2.MessageLoggers.Length);
    }

    private static ILoggerProvider CreateProvider()
    {
        var disposableProvider = new Mock<ILoggerProvider>();
        disposableProvider.As<IDisposable>()
              .Setup(p => p.Dispose());
        return disposableProvider.Object;
    }

    [Fact]
    public static void Dispose_ThrowException_SwallowsException()
    {
        // Arrange
        var factory = Utils.CreateLoggerFactory();
        var throwingProvider = new Mock<ILoggerProvider>();
        throwingProvider.As<IDisposable>()
            .Setup(p => p.Dispose())
            .Throws<Exception>();

        factory.AddProvider(throwingProvider.Object);

        // Act
        factory.Dispose();

        // Assert
        throwingProvider.As<IDisposable>()
            .Verify(p => p.Dispose(), Times.Once());
    }

    private static string GetActivityLogString(ActivityTrackingOptions options)
    {
        Activity activity = Activity.Current!;
        if (activity == null)
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder();
        if ((options & ActivityTrackingOptions.SpanId) != 0)
        {
            sb.Append($"SpanId:{activity.GetSpanId()}");
        }

        if ((options & ActivityTrackingOptions.TraceId) != 0)
        {
            sb.Append(sb.Length > 0 ? $", TraceId:{activity.GetTraceId()}" : $"TraceId:{activity.GetTraceId()}");
        }

        if ((options & ActivityTrackingOptions.ParentId) != 0)
        {
            sb.Append(sb.Length > 0 ? $", ParentId:{activity.GetParentId()}" : $"ParentId:{activity.GetParentId()}");
        }

        if ((options & ActivityTrackingOptions.TraceState) != 0)
        {
            sb.Append(sb.Length > 0 ? $", TraceState:{activity.TraceStateString}" : $"TraceState:{activity.TraceStateString}");
        }

        if ((options & ActivityTrackingOptions.TraceFlags) != 0)
        {
            sb.Append(sb.Length > 0 ? $", TraceFlags:{activity.ActivityTraceFlags}" : $"TraceFlags:{activity.ActivityTraceFlags}");
        }

        return sb.ToString();
    }

    [Theory]
    [InlineData(ActivityTrackingOptions.SpanId)]
    [InlineData(ActivityTrackingOptions.TraceId)]
    [InlineData(ActivityTrackingOptions.ParentId)]
    [InlineData(ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
    [InlineData(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.TraceFlags)]
    public static void TestActivityIds(ActivityTrackingOptions options)
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
            .Configure(o => o.ActivityTrackingOptions = options)
            .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.AddBaggage("baggageTestKey1", "baggageTestValue");
        activity.AddTag("tagTestKey", "tagTestValue");
        activity.Start();
        string activity1String = GetActivityLogString(options);
        string activity2String;

        using (logger.BeginScope("Scope 1"))
        {
            logger.LogInformation("Message 1");
            using Activity b = new Activity("ScopeActivity");
            b.Start();
            activity2String = GetActivityLogString(options);

            using (logger.BeginScope("Scope 2"))
            {
                logger.LogInformation("Message 2");
            }

            b.Stop();
        }

        activity.Stop();

        Assert.Equal(activity1String, loggerProvider.LogText[1]);
        Assert.Equal(activity2String, loggerProvider.LogText[4]);
        Assert.Equal(7, loggerProvider.LogText.Count); // Ensure that Baggage and Tags aren't added.
    }

    [Fact]
    public static void TestInvalidActivityTrackingOptions()
    {
        Assert.Throws<ArgumentException>(() =>
           Utils.CreateLoggerFactory(builder => { builder.Configure(o => o.ActivityTrackingOptions = (ActivityTrackingOptions)0xFF00); }));
    }

    [Fact]
    public static void TestActivityTrackingOptions_ShouldAddBaggageItemsAsNewScope_WhenBaggageOptionIsSet()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
                .Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.Baggage)
                .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.AddBaggage("testKey1", null);
        activity.AddBaggage("testKey2", string.Empty);
        activity.AddBaggage("testKey3", "testValue");
        activity.Start();

        logger.LogInformation("Message1");

        activity.Stop();

        foreach (string s in loggerProvider.LogText)
        {
            System.Console.WriteLine(s);
        }

        Assert.Equal("Message1", loggerProvider.LogText[0]);
        Assert.Equal("testKey3:testValue, testKey2:, testKey1:", loggerProvider.LogText[2]);
    }

    [Fact]
    public static void TestActivityTrackingOptions_ShouldAddTagsAsNewScope_WhenTagsOptionIsSet()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
                .Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.Tags)
                .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.AddTag("testKey1", null);
        activity.AddTag("testKey2", string.Empty);
        activity.AddTag("testKey3", "testValue");
        activity.AddTag("testKey4", new Dummy());
        activity.Start();

        logger.LogInformation("Message1");

        activity.Stop();

        Assert.Equal("Message1", loggerProvider.LogText[0]);
        Assert.Equal("testKey1:, testKey2:, testKey3:testValue, testKey4:DummyToString", loggerProvider.LogText[2]);
    }

    [Fact]
    public static void TestActivityTrackingOptions_ShouldAddTagsAndBaggageAsOneScopeAndTraceIdAsOtherScope_WhenTagsBaggageAndTraceIdOptionAreSet()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
                .Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.Baggage | ActivityTrackingOptions.Tags)
                .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.AddTag("testTagKey1", "testTagValue");
        activity.AddBaggage("testBaggageKey1", "testBaggageValue");
        activity.Start();
        logger.LogInformation("Message1");
        string traceIdActivityLogString = GetActivityLogString(ActivityTrackingOptions.TraceId);
        activity.Stop();

        Assert.Equal("Message1", loggerProvider.LogText[0]);
        Assert.Equal(traceIdActivityLogString, loggerProvider.LogText[1]);
        Assert.Equal("testTagKey1:testTagValue", loggerProvider.LogText[2]);
        Assert.Equal("testBaggageKey1:testBaggageValue", loggerProvider.LogText[3]);
    }

    [Fact]
    public static void TestActivityTrackingOptions_ShouldAddNewTagAndBaggageItemsAtRuntime_WhenTagsAndBaggageOptionAreSetAndWithNestedScopes()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
                .Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.Baggage | ActivityTrackingOptions.Tags)
                .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.Start();

        // Add baggage and tag items before the first log entry.
        activity.AddTag("MyTagKey1", "1");
        activity.AddBaggage("MyBaggageKey1", "1");

        // Log a message, this should create any cached objects.
        logger.LogInformation("Message1");

        // Start the first scope, add some more items and log.
        using (logger.BeginScope("Scope1"))
        {
            activity.AddTag("MyTagKey2", "2");
            activity.AddBaggage("MyBaggageKey2", "2");
            logger.LogInformation("Message2");

            // Add two additional scopes and also replace some tag and baggage items.
            using (logger.BeginScope("Scope2"))
            {
                activity.AddTag("MyTagKey3", "3");
                activity.AddBaggage("MyBaggageKey3", "3");

                using (logger.BeginScope("Scope3"))
                {
                    activity.SetTag("MyTagKey3", "4");
                    activity.SetBaggage("MyBaggageKey3", "4");
                    logger.LogInformation("Message3");
                }
            }

            // Along with this message we expect all baggage and tags items
            // as well as the Scope1 but not the Scope2 and Scope3.
            logger.LogInformation("Message4");

            activity.Stop();
        }

        Assert.Equal("Message1", loggerProvider.LogText[0]);
        Assert.Equal("MyTagKey1:1", loggerProvider.LogText[2]);
        Assert.Equal("MyBaggageKey1:1", loggerProvider.LogText[3]);

        Assert.Equal("Message2", loggerProvider.LogText[4]);
        Assert.Equal("MyTagKey1:1, MyTagKey2:2", loggerProvider.LogText[6]);
        Assert.Equal("MyBaggageKey2:2, MyBaggageKey1:1", loggerProvider.LogText[7]);
        Assert.Equal("Scope1", loggerProvider.LogText[8]);

        Assert.Equal("Message3", loggerProvider.LogText[9]);
        Assert.Equal("MyTagKey1:1, MyTagKey2:2, MyTagKey3:4", loggerProvider.LogText[11]);
        Assert.Equal("MyBaggageKey3:4, MyBaggageKey2:2, MyBaggageKey1:1", loggerProvider.LogText[12]);
        Assert.Equal("Scope1", loggerProvider.LogText[13]);
        Assert.Equal("Scope2", loggerProvider.LogText[14]);
        Assert.Equal("Scope3", loggerProvider.LogText[15]);

        Assert.Equal("Message4", loggerProvider.LogText[16]);
        Assert.Equal("MyTagKey1:1, MyTagKey2:2, MyTagKey3:4", loggerProvider.LogText[18]);
        Assert.Equal("MyBaggageKey3:4, MyBaggageKey2:2, MyBaggageKey1:1", loggerProvider.LogText[19]);
        Assert.Equal("Scope1", loggerProvider.LogText[20]);
    }

    [Fact]
    public static void TestActivityTrackingOptions_ShouldNotAddAdditionalScope_WhenTagsBaggageOptionAreSetButTagsAndBaggageAreEmpty()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();

        using var loggerFactory = Utils.CreateLoggerFactory(builder =>
        {
            builder
                .Configure(o => o.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.Baggage | ActivityTrackingOptions.Tags)
                .AddProvider(loggerProvider);
        });

        var logger = loggerFactory.CreateLogger("Logger");

        using Activity activity = new Activity("ScopeActivity");
        activity.Start();
        logger.LogInformation("Message1");
        string traceIdActivityLogString = GetActivityLogString(ActivityTrackingOptions.TraceId);
        activity.Stop();

        Assert.Equal("Message1", loggerProvider.LogText[0]);
        Assert.Equal(traceIdActivityLogString, loggerProvider.LogText[1]);
        Assert.Equal(2, loggerProvider.LogText.Count); // Ensure that the additional scopes for tags and baggage aren't added.
    }

    [Fact]
    public static void CallsSetScopeProvider_OnSupportedProviders()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();
        using var loggerFactory = Utils.CreateLoggerFactory();
        loggerFactory.AddProvider(loggerProvider);

        var logger = loggerFactory.CreateLogger("Logger");

        using (logger.BeginScope("Scope"))
        {
            using (logger.BeginScope("Scope2"))
            {
                logger.LogInformation("Message");
            }
        }

        logger.LogInformation("Message2");

        Assert.Equal(loggerProvider.LogText,
            new[]
            {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
            });
        Assert.NotNull(loggerProvider.ScopeProvider);
        Assert.Equal(0, loggerProvider.BeginScopeCalledTimes);
    }

    [Fact]
    public static void BeginScope_ReturnsExternalSourceTokenDirectly()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();
        using var loggerFactory = Utils.CreateLoggerFactory();
        loggerFactory.AddProvider(loggerProvider);

        var logger = loggerFactory.CreateLogger("Logger");

        var scope = logger.BeginScope("Scope");
        Assert.StartsWith(loggerProvider.ScopeProvider!.GetType().FullName, scope!.GetType().FullName);
    }

    [Fact]
    public static void BeginScope_ReturnsInternalSourceTokenDirectly()
    {
        using var loggerProvider = new InternalScopeLoggerProvider();
        using var loggerFactory = Utils.CreateLoggerFactory();
        loggerFactory.AddProvider(loggerProvider);

        var logger = loggerFactory.CreateLogger("Logger");
        var scope = logger.BeginScope("Scope");
        Assert.Contains("LoggerExternalScopeProvider+Scope", scope!.GetType().FullName);
    }

    [Fact]
    public static void BeginScope_ReturnsCompositeToken_ForMultipleLoggers()
    {
        using var loggerProvider = new ExternalScopeLoggerProvider();
        using var loggerProvider2 = new InternalScopeLoggerProvider();
        using var loggerFactory = Utils.CreateLoggerFactory();
        loggerFactory.AddProvider(loggerProvider);
        loggerFactory.AddProvider(loggerProvider2);

        var logger = loggerFactory.CreateLogger("Logger");

        using (logger.BeginScope("Scope"))
        {
            using (logger.BeginScope("Scope2"))
            {
                logger.LogInformation("Message");
            }
        }

        logger.LogInformation("Message2");

        Assert.Equal(loggerProvider.LogText,
            new[]
            {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
            });

        Assert.Equal(loggerProvider2.LogText,
            new[]
            {
                    "Message",
                    "Scope",
                    "Scope2",
                    "Message2",
            });
    }

    [Fact]
    public static void CreateDisposeDisposesInnerServiceProvider()
    {
        var disposed = false;
        var provider = new Mock<ILoggerProvider>();
        provider.Setup(p => p.Dispose()).Callback(() => disposed = true);

        var factory = Utils.CreateLoggerFactory(builder => builder.Services.AddSingleton(_ => provider.Object));
        factory.Dispose();

        Assert.True(disposed);
    }

    private class InternalScopeLoggerProvider : ILoggerProvider, ILogger
    {
        private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
        public List<string> LogText { get; set; } = [];

        public void Dispose()
        {
            // nop
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogText.Add(formatter(state, exception));
            _scopeProvider.ForEachScope((scope, builder) => builder.Add(scope!.ToString()!), LogText);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            return _scopeProvider.Push(state);
        }
    }

    private class ExternalScopeLoggerProvider : ILoggerProvider, ISupportExternalScope, ILogger
    {
        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            ScopeProvider = scopeProvider;
        }

        public IExternalScopeProvider? ScopeProvider { get; set; }
        public int BeginScopeCalledTimes { get; set; }
        public List<string> LogText { get; set; } = [];
        public void Dispose()
        {
            // nop
        }

        public ILogger CreateLogger(string categoryName)
        {
            return this;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LogText.Add(formatter(state, exception));

            // Notice that other ILoggers maybe not call "ToString()" on the scope but enumerate it and this isn't covered by this implementation.
            // E.g. the SimpleConsoleFormatter calls "ToString()" like it's done here but the "JsonConsoleFormatter" enumerates a scope
            // if the Scope is of type IEnumerable<KeyValuePair<string, object>>.
            ScopeProvider!.ForEachScope((scope, builder) => builder.Add(scope!.ToString()!), LogText);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            BeginScopeCalledTimes++;
            return null;
        }
    }

    private class Dummy
    {
        public override string ToString()
        {
            return "DummyToString";
        }
    }

    private sealed class Provider : ILoggerProvider
    {
        public FakeLogger? Logger { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            Logger = new FakeLogger((FakeLogCollector?)null, categoryName);
            return Logger;
        }

        public void Dispose()
        {
            // nothing to do
        }
    }
}

[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Needs to be top-level since it has extension methods")]
internal static class ActivityExtensions
{
    public static string GetSpanId(this Activity activity)
    {
        return activity.IdFormat switch
        {
            ActivityIdFormat.Hierarchical => activity.Id,
            ActivityIdFormat.W3C => activity.SpanId.ToHexString(),
            _ => null,
        }

        ?? string.Empty;
    }

    public static string GetTraceId(this Activity activity)
    {
        return activity.IdFormat switch
        {
            ActivityIdFormat.Hierarchical => activity.RootId,
            ActivityIdFormat.W3C => activity.TraceId.ToHexString(),
            _ => null,
        }

        ?? string.Empty;
    }

    public static string GetParentId(this Activity activity)
    {
        return activity.IdFormat switch
        {
            ActivityIdFormat.Hierarchical => activity.ParentId,
            ActivityIdFormat.W3C => activity.ParentSpanId.ToHexString(),
            _ => null,
        }

        ?? string.Empty;
    }
}
