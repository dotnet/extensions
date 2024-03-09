// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Contextual.Internal;
using Microsoft.Extensions.Options.Contextual.Provider;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Options.Contextual.Test;

public class ContextualOptionsFactoryTests
{
    [Fact]
    public async Task ContextualOptionsFactoryDoesNothingWithNoOptionalDependenciesProvided()
    {
        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            Enumerable.Empty<ILoadContextualOptions<List<string>>>(),
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        var result = await new ContextualOptions<List<string>, IOptionsContext>(sut).GetAsync(Mock.Of<IOptionsContext>(), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task DefaultValidatorsFailValidationForAnyInstanceName()
    {
        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            Enumerable.Empty<ILoadContextualOptions<List<string>>>(),
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            new[] { new ValidateContextualOptions<List<string>>(null, _ => false, "epic fail") });

        await Assert.ThrowsAsync<OptionsValidationException>(async () => await sut.CreateAsync(string.Empty, Mock.Of<IOptionsContext>(), default));
        await Assert.ThrowsAsync<OptionsValidationException>(async () => await sut.CreateAsync("A Name", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    public async Task NamedValidatorsFailValidationOnlyForNamedInstance()
    {
        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            Enumerable.Empty<ILoadContextualOptions<List<string>>>(),
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            new[] { new ValidateContextualOptions<List<string>>("Foo", _ => false, "epic fail") });

        await Assert.ThrowsAsync<OptionsValidationException>(async () => await sut.CreateAsync("Foo", Mock.Of<IOptionsContext>(), default));
        Assert.Empty(await sut.CreateAsync("Bar", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    public async Task PostConfigureRunsAfterLoad()
    {
        var loaders = new[]
        {
                new LoadContextualOptions<List<string>>(
                string.Empty,
                (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("configure"), context))),
            };

        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            new[] { new PostConfigureContextualOptions<List<string>>(string.Empty, (_, list) => list.Add("post configure")) },
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        var result = await sut.CreateAsync(string.Empty, Mock.Of<IOptionsContext>(), default);

        Assert.Equal(new[] { "configure", "post configure" }, result);
    }

    [Fact]
    public async Task NamedLoadersLoadOnlyNamedOptions()
    {
        var loaders = new[]
        {
                new LoadContextualOptions<List<string>>(
                "Foo",
                (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("configure"), context)))
            };

        var sut = new ContextualOptions<List<string>, IOptionsContext>(new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>()));

        Assert.Equal(new[] { "configure" }, await sut.GetAsync("Foo", Mock.Of<IOptionsContext>(), default));
        Assert.Empty(await sut.GetAsync("Bar", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    public async Task DefaultLoadersLoadAllOptions()
    {
        var loaders = new[]
        {
                new LoadContextualOptions<List<string>>(
                null,
                (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("configure"), context))),
            };

        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        Assert.Equal(new[] { "configure" }, await sut.CreateAsync("Foo", Mock.Of<IOptionsContext>(), default));
        Assert.Equal(new[] { "configure" }, await sut.CreateAsync("Bar", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    public async Task DefaultPostConfigureConfiguresAllOptions()
    {
        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            Enumerable.Empty<ILoadContextualOptions<List<string>>>(),
            new[] { new PostConfigureContextualOptions<List<string>>(null, (_, list) => list.Add("post configure")) },
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        Assert.Equal(new[] { "post configure" }, await sut.CreateAsync("Foo", Mock.Of<IOptionsContext>(), default));
        Assert.Equal(new[] { "post configure" }, await sut.CreateAsync("Bar", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    public async Task NamePostConfigureConfiguresOnlyNamedOptions()
    {
        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            Enumerable.Empty<ILoadContextualOptions<List<string>>>(),
            new[] { new PostConfigureContextualOptions<List<string>>("Foo", (_, list) => list.Add("post configure")) },
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        Assert.Equal(new[] { "post configure" }, await sut.CreateAsync("Foo", Mock.Of<IOptionsContext>(), default));
        Assert.Empty(await sut.CreateAsync("Bar", Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    [SuppressMessage(
        "Minor Code Smell",
        "S3257:Declarations and initializations should be as concise as possible",
        Justification = "This analyzer is broken. It's not actually redundant.")]
    public async Task LoadsRunConcurrentlyWhileConfiguresRunSequentially()
    {
        using var semaphore = new SemaphoreSlim(0);

        var loaders = new LoadContextualOptions<List<string>>[]
        {
                new(string.Empty, (context, _) =>
                    {
                        semaphore.Release();
                        return new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("1"), context));
                    }),
                new(string.Empty, async (context, cancellationToken) =>
                    {
                        await semaphore.WaitAsync(3, cancellationToken);
                        return new ConfigureContextualOptions<List<string>>((_, list) => list.Add("2"), context);
                    }),
                new(string.Empty, (context, _) =>
                    {
                        semaphore.Release();
                        return new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("3"), context));
                    }),
                new(string.Empty, (context, _) =>
                    {
                        semaphore.Release();
                        return new ValueTask<IConfigureContextualOptions<List<string>>>(new ConfigureContextualOptions<List<string>>((_, list) => list.Add("4"), context));
                    }),
        };

        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        Assert.Equal(new[] { "1", "2", "3", "4" }, await sut.CreateAsync(string.Empty, Mock.Of<IOptionsContext>(), default));
    }

    [Fact]
    [SuppressMessage(
        "Minor Code Smell",
        "S3257:Declarations and initializations should be as concise as possible",
        Justification = "This analyzer is broken. It's not actually redundant.")]
    public async Task CreateAsyncAggregatesAllExceptions()
    {
        var loaders = new LoadContextualOptions<List<string>>[]
        {
                new(string.Empty, (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(Mock.Of<IConfigureContextualOptions<List<string>>>(MockBehavior.Strict))),
                new(string.Empty, (context, _) => throw new NotSupportedException()),
        };

        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await sut.CreateAsync(string.Empty, Mock.Of<IOptionsContext>(), default));
        Assert.Equal(2, exception.InnerExceptions.Count);
    }

    [Fact]
    [SuppressMessage(
        "Minor Code Smell",
        "S3257:Declarations and initializations should be as concise as possible",
        Justification = "This analyzer is broken. It's not actually redundant.")]
    public async Task CreateAsyncCallsDisposeEvenAfterExceptions()
    {
        var disposeMock = new Mock<IConfigureContextualOptions<List<string>>>();
        disposeMock.Setup(conf => conf.Dispose()).Throws(new ObjectDisposedException("foo"));
        var loaders = new LoadContextualOptions<List<string>>[]
        {
                new(string.Empty, (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(Mock.Of<IConfigureContextualOptions<List<string>>>(MockBehavior.Strict))),
                new(string.Empty, (context, _) => new ValueTask<IConfigureContextualOptions<List<string>>>(disposeMock.Object)),
        };

        var sut = new ContextualOptionsFactory<List<string>>(
            new OptionsFactory<List<string>>(Enumerable.Empty<IConfigureOptions<List<string>>>(), Enumerable.Empty<IPostConfigureOptions<List<string>>>()),
            loaders,
            Enumerable.Empty<IPostConfigureContextualOptions<List<string>>>(),
            Enumerable.Empty<IValidateContextualOptions<List<string>>>());

        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await sut.CreateAsync(string.Empty, Mock.Of<IOptionsContext>(), default));
        Assert.Equal(2, exception.InnerExceptions.Count);
        disposeMock.Verify(conf => conf.Dispose(), Times.Once);
    }
}
