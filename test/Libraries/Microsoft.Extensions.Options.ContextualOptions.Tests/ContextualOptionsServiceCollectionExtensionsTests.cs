﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Options.Contextual.Test;

public class ContextualOptionsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddContextualOptionsTest()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddContextualOptions());

        using var provider = new ServiceCollection().AddContextualOptions().BuildServiceProvider();

        Assert.IsType<ContextualOptions<object>>(provider.GetRequiredService<IContextualOptions<object>>());
        Assert.IsType<ContextualOptions<object>>(provider.GetRequiredService<INamedContextualOptions<object>>());
        Assert.IsType<ContextualOptionsFactory<object>>(provider.GetRequiredService<IContextualOptionsFactory<object>>());
    }

    [Fact]
    public void ConfigureWithLoadTest()
    {
        Func<IOptionsContext, CancellationToken, ValueTask<IConfigureContextualOptions<string>>> loadOptions =
            (_, _) => new ValueTask<IConfigureContextualOptions<string>>(NullConfigureContextualOptions.GetInstance<string>());

        using var provider = new ServiceCollection().Configure(loadOptions).BuildServiceProvider();
        var loader = (LoadContextualOptions<string>)provider.GetRequiredService<ILoadContextualOptions<string>>();
        Assert.Equal(loadOptions, loader.LoadAction);
        Assert.Equal(string.Empty, loader.Name);
    }

    [Fact]
    public async Task ConfigureDirectTest()
    {
        Action<IOptionsContext, string> configureOptions = (_, _) => { };
        using var provider = new ServiceCollection().Configure(configureOptions).BuildServiceProvider();
        var loader = (LoadContextualOptions<string>)provider.GetRequiredService<ILoadContextualOptions<string>>();
        Assert.Equal(configureOptions, ((ConfigureContextualOptions<string>)await loader.LoadAction(Mock.Of<IOptionsContext>(), default)).ConfigureOptions);
        Assert.Equal(string.Empty, loader.Name);
    }

    [Fact]
    public void PostConfigureAllTest()
    {
        Action<IOptionsContext, string> configureOptions = (_, _) => { };
        using var provider = new ServiceCollection().PostConfigureAll(configureOptions).BuildServiceProvider();
        var postConfigure = (PostConfigureContextualOptions<string>)provider.GetRequiredService<IPostConfigureContextualOptions<string>>();

        Assert.Equal(configureOptions, postConfigure.Action);
        Assert.Null(postConfigure.Name);
    }

    [Fact]
    public void PostConfigureDefaultTest()
    {
        Action<IOptionsContext, string> configureOptions = (_, _) => { };
        using var provider = new ServiceCollection().PostConfigure(configureOptions).BuildServiceProvider();
        var postConfigure = (PostConfigureContextualOptions<string>)provider.GetRequiredService<IPostConfigureContextualOptions<string>>();

        Assert.Equal(configureOptions, postConfigure.Action);
        Assert.Equal(string.Empty, postConfigure.Name);
    }

    [Fact]
    public void PostConfigureNamedTest()
    {
        Action<IOptionsContext, string> configureOptions = (_, _) => { };
        using var provider = new ServiceCollection().PostConfigure("Foo", configureOptions).BuildServiceProvider();
        var postConfigure = (PostConfigureContextualOptions<string>)provider.GetRequiredService<IPostConfigureContextualOptions<string>>();

        Assert.Equal(configureOptions, postConfigure.Action);
        Assert.Equal("Foo", postConfigure.Name);
    }

    [Fact]
    public void ValidateDefaultTest()
    {
        Func<string, bool> validate = _ => true;
        using var provider = new ServiceCollection().ValidateContextualOptions(validate, "epic fail").BuildServiceProvider();
        var validateOptions = (ValidateContextualOptions<string>)provider.GetRequiredService<IValidateContextualOptions<string>>();

        Assert.Equal(validate, validateOptions.Validation);
        Assert.Equal(string.Empty, validateOptions.Name);
    }

    [Fact]
    public void ValidateNamedTest()
    {
        Func<string, bool> validate = _ => true;
        using var provider = new ServiceCollection().ValidateContextualOptions("Foo", validate, "epic fail").BuildServiceProvider();
        var validateOptions = (ValidateContextualOptions<string>)provider.GetRequiredService<IValidateContextualOptions<string>>();

        Assert.Equal(validate, validateOptions.Validation);
        Assert.Equal("Foo", validateOptions.Name);
    }
}
