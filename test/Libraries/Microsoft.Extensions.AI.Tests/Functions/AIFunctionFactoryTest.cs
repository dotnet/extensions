﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S3358 // Ternary operators should not be nested

namespace Microsoft.Extensions.AI;

public class AIFunctionFactoryTest
{
    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!));
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!, target: new object()));
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!, target: new object(), name: "myAiFunk"));
        Assert.Throws<ArgumentNullException>("target", () => AIFunctionFactory.Create(typeof(AIFunctionFactoryTest).GetMethod(nameof(InvalidArguments_Throw))!, null));
        Assert.Throws<ArgumentException>("method", () => AIFunctionFactory.Create(typeof(List<>).GetMethod("Add")!, new List<int>()));
    }

    [Fact]
    public async Task Parameters_MappedByName_Async()
    {
        AIFunction func;

        func = AIFunctionFactory.Create((string a) => a + " " + a);
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync(new() { ["a"] = "test" }));

        func = AIFunctionFactory.Create((string a, string b) => b + " " + a);
        AssertExtensions.EqualFunctionCallResults("hello world", await func.InvokeAsync(new() { ["b"] = "hello", ["a"] = "world" }));

        func = AIFunctionFactory.Create((int a, long b) => a + b);
        AssertExtensions.EqualFunctionCallResults(3L, await func.InvokeAsync(new() { ["a"] = 1, ["b"] = 2L }));
    }

    [Fact]
    public async Task Parameters_DefaultValuesAreUsedButOverridable_Async()
    {
        AIFunction func = AIFunctionFactory.Create((string a = "test") => a + " " + a);
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync());
        AssertExtensions.EqualFunctionCallResults("hello hello", await func.InvokeAsync(new() { ["a"] = "hello" }));
    }

    [Fact]
    public async Task Parameters_MissingRequiredParametersFail_Async()
    {
        AIFunction[] funcs =
        [
            AIFunctionFactory.Create((string theParam) => theParam + " " + theParam),
            AIFunctionFactory.Create((string? theParam) => theParam + " " + theParam),
            AIFunctionFactory.Create((int theParam) => theParam * 2),
            AIFunctionFactory.Create((int? theParam) => theParam * 2),
        ];

        foreach (AIFunction f in funcs)
        {
            Exception e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync());
            Assert.Contains("'theParam'", e.Message);
        }
    }

    [Fact]
    public async Task Parameters_MappedByType_Async()
    {
        using var cts = new CancellationTokenSource();

        foreach (CancellationToken ctArg in new[] { cts.Token, default })
        {
            CancellationToken written = default;
            AIFunction func = AIFunctionFactory.Create((int value1 = 1, string value2 = "2", CancellationToken cancellationToken = default) =>
            {
                written = cancellationToken;
                return 42;
            });
            AssertExtensions.EqualFunctionCallResults(42, await func.InvokeAsync(cancellationToken: ctArg));
            Assert.Equal(ctArg, written);
            Assert.DoesNotContain("cancellationToken", func.JsonSchema.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Returns_AsyncReturnTypesSupported_Async()
    {
        AIFunction func;

        func = AIFunctionFactory.Create(Task<string> (string a) => Task.FromResult(a + " " + a));
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync(new() { ["a"] = "test" }));

        func = AIFunctionFactory.Create(ValueTask<string> (string a, string b) => new ValueTask<string>(b + " " + a));
        AssertExtensions.EqualFunctionCallResults("hello world", await func.InvokeAsync(new() { ["b"] = "hello", ["a"] = "world" }));

        long result = 0;
        func = AIFunctionFactory.Create(async Task (int a, long b) => { result = a + b; await Task.Yield(); });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(new() { ["a"] = 1, ["b"] = 2L }));
        Assert.Equal(3, result);

        result = 0;
        func = AIFunctionFactory.Create(async ValueTask (int a, long b) => { result = a + b; await Task.Yield(); });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(new() { ["a"] = 1, ["b"] = 2L }));
        Assert.Equal(3, result);

        func = AIFunctionFactory.Create((int count) => SimpleIAsyncEnumerable(count));
        AssertExtensions.EqualFunctionCallResults(new int[] { 0, 1, 2, 3, 4 }, await func.InvokeAsync(new() { ["count"] = 5 }));

        static async IAsyncEnumerable<int> SimpleIAsyncEnumerable(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }

        func = AIFunctionFactory.Create(() => (IAsyncEnumerable<int>)new ThrowingAsyncEnumerable());
        await Assert.ThrowsAsync<NotImplementedException>(() => func.InvokeAsync());
    }

    private sealed class ThrowingAsyncEnumerable : IAsyncEnumerable<int>
    {
#pragma warning disable S3717 // Track use of "NotImplementedException"
        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new NotImplementedException();
#pragma warning restore S3717 // Track use of "NotImplementedException"
    }

    [Fact]
    public void Metadata_DerivedFromLambda()
    {
        AIFunction func;

        Func<string> dotnetFunc = () => "test";
        func = AIFunctionFactory.Create(dotnetFunc);
        Assert.Contains("Metadata_DerivedFromLambda", func.Name);
        Assert.Empty(func.Description);
        Assert.Same(dotnetFunc.Method, func.UnderlyingMethod);

        Func<string, string> dotnetFunc2 = a => a + " " + a;
        func = AIFunctionFactory.Create(dotnetFunc2);
        Assert.Contains("Metadata_DerivedFromLambda", func.Name);
        Assert.Empty(func.Description);
        Assert.Same(dotnetFunc2.Method, func.UnderlyingMethod);

        Func<string, string, string> dotnetFunc3 = [Description("This is a test function")] ([Description("This is A")] string a, [Description("This is B")] string b) => b + " " + a;
        func = AIFunctionFactory.Create(dotnetFunc3);
        Assert.Contains("Metadata_DerivedFromLambda", func.Name);
        Assert.Equal("This is a test function", func.Description);
        Assert.Same(dotnetFunc3.Method, func.UnderlyingMethod);
        Assert.Collection(func.UnderlyingMethod!.GetParameters(),
            p => Assert.Equal("This is A", p.GetCustomAttribute<DescriptionAttribute>()?.Description),
            p => Assert.Equal("This is B", p.GetCustomAttribute<DescriptionAttribute>()?.Description));
    }

    [Fact]
    public void AIFunctionFactoryCreateOptions_ValuesPropagateToAIFunction()
    {
        IReadOnlyDictionary<string, object?> metadata = new Dictionary<string, object?> { ["a"] = "b" };

        AIFunctionFactoryOptions.ArgumentBinderFunc binder = (ParameterInfo p, AIFunctionArguments a, out object? value) =>
        {
            value = null;
            return false;
        };

        var options = new AIFunctionFactoryOptions
        {
            Name = "test name",
            Description = "test description",
            AdditionalProperties = metadata,
            ArgumentBinder = binder,
        };

        Assert.Equal("test name", options.Name);
        Assert.Equal("test description", options.Description);
        Assert.Same(metadata, options.AdditionalProperties);
        Assert.Same(binder, options.ArgumentBinder);

        Action dotnetFunc = () => { };
        AIFunction func = AIFunctionFactory.Create(dotnetFunc, options);

        Assert.Equal("test name", func.Name);
        Assert.Equal("test description", func.Description);
        Assert.Same(dotnetFunc.Method, func.UnderlyingMethod);
        Assert.Equal(metadata, func.AdditionalProperties);
    }

    [Fact]
    public void AIFunctionFactoryOptions_DefaultValues()
    {
        AIFunctionFactoryOptions options = new();

        Assert.Null(options.Name);
        Assert.Null(options.Description);
        Assert.Null(options.AdditionalProperties);
        Assert.Null(options.SerializerOptions);
        Assert.Null(options.JsonSchemaCreateOptions);
        Assert.Null(options.ArgumentBinder);
    }

    [Fact]
    public async Task AIFunctionFactoryOptions_SupportsSkippingParameters()
    {
        AIFunction func = AIFunctionFactory.Create(
            (string firstParameter, int secondParameter) => firstParameter + secondParameter,
            new()
            {
                JsonSchemaCreateOptions = new()
                {
                    IncludeParameter = p => p.Name != "firstParameter",
                }
            });

        Assert.DoesNotContain("firstParameter", func.JsonSchema.ToString());
        Assert.Contains("secondParameter", func.JsonSchema.ToString());

        JsonElement? result = (JsonElement?)await func.InvokeAsync(new()
        {
            ["firstParameter"] = "test",
            ["secondParameter"] = 42
        });
        Assert.NotNull(result);
        Assert.Contains("test42", result.ToString());
    }

    [Fact]
    public async Task AIFunctionArguments_SatisfiesParameters()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunctionArguments arguments = new() { ["myInteger"] = 42 };

        AIFunction func = AIFunctionFactory.Create((
            int myInteger,
            IServiceProvider services1,
            IServiceProvider services2,
            AIFunctionArguments arguments1,
            AIFunctionArguments arguments2,
            IServiceProvider? services3,
            AIFunctionArguments? arguments3,
            IServiceProvider? services4 = null,
            AIFunctionArguments? arguments4 = null) =>
        {
            Assert.Same(sp, services1);
            Assert.Same(sp, services2);
            Assert.Same(sp, services3);
            Assert.Same(sp, services4);

            Assert.Same(arguments, arguments1);
            Assert.Same(arguments, arguments2);
            Assert.Same(arguments, arguments3);
            Assert.Same(arguments, arguments4);

            return myInteger;
        });

        Assert.Contains("myInteger", func.JsonSchema.ToString());
        Assert.DoesNotContain("services", func.JsonSchema.ToString());
        Assert.DoesNotContain("arguments", func.JsonSchema.ToString());

        await Assert.ThrowsAsync<ArgumentException>("arguments", () => func.InvokeAsync(arguments));

        arguments.Services = sp;
        var result = await func.InvokeAsync(arguments);

        Assert.Contains("42", result?.ToString());
    }

    [Fact]
    public async Task AIFunctionArguments_MissingServicesMayBeOptional()
    {
        ServiceCollection sc = new();
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction func = AIFunctionFactory.Create((
            int? myInteger = null,
            AIFunctionArguments? arguments = null,
            IServiceProvider? services = null) =>
        {
            Assert.NotNull(arguments);
            Assert.Null(services);
            return myInteger;
        });

        Assert.Contains("myInteger", func.JsonSchema.ToString());
        Assert.DoesNotContain("services", func.JsonSchema.ToString());
        Assert.DoesNotContain("arguments", func.JsonSchema.ToString());

        var result = await func.InvokeAsync(new() { ["myInteger"] = 42 });
        Assert.Contains("42", result?.ToString());

        result = await func.InvokeAsync();
        Assert.Equal("", result?.ToString());
    }

    [Fact]
    public async Task ArgumentBinderFunc_CanBeUsedToSupportFromKeyedServices()
    {
        MyService service = new(42);

        ServiceCollection sc = new();
        sc.AddKeyedSingleton("key", service);
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(
            ([FromKeyedServices("key")] MyService service, int myInteger) => service.Value + myInteger,
            new AIFunctionFactoryOptions
            {
                JsonSchemaCreateOptions = new()
                {
                    IncludeParameter = p => p.GetCustomAttribute<FromKeyedServicesAttribute>() is null,
                },
                ArgumentBinder = (ParameterInfo p, AIFunctionArguments a, out object? value) =>
                {
                    if (p.GetCustomAttribute<FromKeyedServicesAttribute>() is { } attr)
                    {
                        value =
                            (a.Services as IKeyedServiceProvider)?.GetKeyedService(p.ParameterType, attr.Key) is { } service ? service :
                            p.HasDefaultValue ? p.DefaultValue :
                            throw new ArgumentException($"Unable to resolve argument for '{p.Name}'.");
                        return true;
                    }

                    value = null;
                    return false;
                },
            });

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        Exception e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync(new() { ["myInteger"] = 1 }));
        Assert.Contains("Unable to resolve", e.Message);

        var result = await f.InvokeAsync(new() { ["myInteger"] = 1, Services = sp });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task ArgumentBinderFunc_CanBeUsedToSupportFromContext()
    {
        MyService service = new(42);

        AIFunction f = AIFunctionFactory.Create(
            (MyService service, int myInteger) => service.Value + myInteger,
            new AIFunctionFactoryOptions
            {
                JsonSchemaCreateOptions = new()
                {
                    IncludeParameter = p => p.ParameterType != typeof(MyService),
                },
                ArgumentBinder = (ParameterInfo p, AIFunctionArguments a, out object? value) =>
                {
                    if (p.ParameterType == typeof(MyService))
                    {
                        value =
                            a.Context?.TryGetValue(typeof(MyService), out object? service) is true ? service :
                            throw new ArgumentException($"Unable to resolve argument for '{p.Name}'.");
                        return true;
                    }

                    value = null;
                    return false;
                },
            });

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        Exception e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync(new() { ["myInteger"] = 1 }));
        Assert.Contains("Unable to resolve", e.Message);

        e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync(new()
        {
            ["myInteger"] = 1,
            Context = new Dictionary<object, object?>(),
        }));
        Assert.Contains("Unable to resolve", e.Message);

        var result = await f.InvokeAsync(new()
        {
            ["myInteger"] = 1,
            Context = new Dictionary<object, object?>
            {
                [typeof(MyService)] = service
            },
        });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task ArgumentBinderFunc_CanBeUsedToOverrideServiceProvider()
    {
        IServiceProvider sp1 = new ServiceCollection().AddSingleton(new MyService(42)).BuildServiceProvider();
        IServiceProvider sp2 = new ServiceCollection().AddSingleton(new MyService(43)).BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(
            (IServiceProvider services) => services.GetRequiredService<MyService>().Value,
            new AIFunctionFactoryOptions
            {
                ArgumentBinder = (ParameterInfo p, AIFunctionArguments a, out object? value) =>
                {
                    if (p.ParameterType == typeof(IServiceProvider))
                    {
                        value = sp2;
                        return true;
                    }

                    value = null;
                    return false;
                },
            });

        var result = await f.InvokeAsync(new() { Services = sp1 });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task ArgumentBinderFunc_CanBeUsedToOverrideAIFunctionArguments()
    {
        AIFunctionArguments args1 = new() { ["a"] = 42 };
        AIFunctionArguments args2 = new() { ["a"] = 43 };

        AIFunction f = AIFunctionFactory.Create(
            (AIFunctionArguments args) => (int)args["a"]!,
            new AIFunctionFactoryOptions
            {
                ArgumentBinder = (ParameterInfo p, AIFunctionArguments a, out object? value) =>
                {
                    if (p.ParameterType == typeof(AIFunctionArguments))
                    {
                        value = args2;
                        return true;
                    }

                    value = null;
                    return false;
                },
            });

        var result = await f.InvokeAsync(args1);
        Assert.Contains("43", result?.ToString());
    }

    private sealed class MyService(int value)
    {
        public int Value => value;
    }
}
