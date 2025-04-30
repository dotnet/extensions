// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.AI.Templates.Tests;

public abstract class TemplateTestBase<TSelf> : IClassFixture<TemplateTestBase<TSelf>.Fixture>, IDisposable
    where TSelf : TemplateTestBase<TSelf>, ITemplateConfigurationProvider
{
    private readonly Fixture _fixture;

    protected ITestOutputHelper OutputHelper { get; }

    protected TemplateTestBase(Fixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        fixture.SetCurrentTestOutputHelper(outputHelper);

        OutputHelper = outputHelper;
    }

    public Fixture GetFixture() => _fixture;

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _fixture.SetCurrentTestOutputHelper(null);
    }

    public sealed class Fixture(IMessageSink messageSink) : TemplateTestFixture(TSelf.Configuration, messageSink);
}

