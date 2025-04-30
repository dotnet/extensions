// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

[Collection(TemplateExecutionTestCollection.Name)]
public abstract class TemplateExecutionTestBase<TConfiguration> : IClassFixture<TemplateExecutionTestBase<TConfiguration>.TemplateExecutionTestFixture>, IDisposable
    where TConfiguration : ITemplateExecutionTestConfigurationProvider
{
    private bool _disposed;

    protected TemplateExecutionTestFixture Fixture { get; }

    protected ITestOutputHelper OutputHelper { get; }

    protected TemplateExecutionTestBase(TemplateExecutionTestFixture fixture, ITestOutputHelper outputHelper)
    {
        Fixture = fixture;
        Fixture.SetCurrentTestOutputHelper(outputHelper);

        OutputHelper = outputHelper;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            Fixture.SetCurrentTestOutputHelper(null);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public sealed class TemplateExecutionTestFixture(IMessageSink messageSink)
        : TemplateExecutionTestClassFixtureBase(TConfiguration.Configuration, messageSink);
}

