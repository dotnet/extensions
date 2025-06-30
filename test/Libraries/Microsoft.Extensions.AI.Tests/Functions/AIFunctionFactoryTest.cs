// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable IDE0004 // Remove Unnecessary Cast
#pragma warning disable S107 // Methods should not have too many parameters
#pragma warning disable S2760 // Sequential tests should not check the same condition
#pragma warning disable S3358 // Ternary operators should not be nested
#pragma warning disable S5034 // "ValueTask" should be consumed correctly

namespace Microsoft.Extensions.AI;

public partial class AIFunctionFactoryTest
{
    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!));
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!, target: new object()));
        Assert.Throws<ArgumentNullException>("method", () => AIFunctionFactory.Create(method: null!, target: new object(), name: "myAiFunk"));
        Assert.Throws<ArgumentNullException>("target", () => AIFunctionFactory.Create(typeof(AIFunctionFactoryTest).GetMethod(nameof(InvalidArguments_Throw))!, (object?)null));
        Assert.Throws<ArgumentNullException>("createInstanceFunc", () =>
            AIFunctionFactory.Create(typeof(AIFunctionFactoryTest).GetMethod(nameof(InvalidArguments_Throw))!, (Func<AIFunctionArguments, object>)null!));
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
            Exception e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync().AsTask());
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
        Assert.Equal("""{"type":"string"}""", func.ReturnJsonSchema.ToString());
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync(new() { ["a"] = "test" }));

        func = AIFunctionFactory.Create(ValueTask<string> (string a, string b) => new ValueTask<string>(b + " " + a));
        Assert.Equal("""{"type":"string"}""", func.ReturnJsonSchema.ToString());
        AssertExtensions.EqualFunctionCallResults("hello world", await func.InvokeAsync(new() { ["b"] = "hello", ["a"] = "world" }));

        long result = 0;
        func = AIFunctionFactory.Create(async Task (int a, long b) => { result = a + b; await Task.Yield(); });
        Assert.Null(func.ReturnJsonSchema);
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(new() { ["a"] = 1, ["b"] = 2L }));
        Assert.Equal(3, result);

        result = 0;
        func = AIFunctionFactory.Create(async ValueTask (int a, long b) => { result = a + b; await Task.Yield(); });
        Assert.Null(func.ReturnJsonSchema);
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(new() { ["a"] = 1, ["b"] = 2L }));
        Assert.Equal(3, result);

        func = AIFunctionFactory.Create((int count) => SimpleIAsyncEnumerable(count), serializerOptions: JsonContext.Default.Options);
        Assert.Equal("""{"type":"array","items":{"type":"integer"}}""", func.ReturnJsonSchema.ToString());
        AssertExtensions.EqualFunctionCallResults(new int[] { 0, 1, 2, 3, 4 }, await func.InvokeAsync(new() { ["count"] = 5 }), JsonContext.Default.Options);

        static async IAsyncEnumerable<int> SimpleIAsyncEnumerable(int count)
        {
            for (int i = 0; i < count; i++)
            {
                await Task.Yield();
                yield return i;
            }
        }

        func = AIFunctionFactory.Create(() => (IAsyncEnumerable<int>)new ThrowingAsyncEnumerable(), serializerOptions: JsonContext.Default.Options);
        await Assert.ThrowsAsync<NotImplementedException>(() => func.InvokeAsync().AsTask());
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

        Func<string, string, string> dotnetFunc3 = [Description("This is a test function")] ([Description("This is A")] a, [Description("This is B")] b) => b + " " + a;
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

        Func<ParameterInfo, AIFunctionFactoryOptions.ParameterBindingOptions> getBindParameterMode = _ => default;

        var options = new AIFunctionFactoryOptions
        {
            Name = "test name",
            Description = "test description",
            AdditionalProperties = metadata,
            ConfigureParameterBinding = getBindParameterMode,
        };

        Assert.Equal("test name", options.Name);
        Assert.Equal("test description", options.Description);
        Assert.Same(metadata, options.AdditionalProperties);
        Assert.Same(getBindParameterMode, options.ConfigureParameterBinding);

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
        Assert.Null(options.ConfigureParameterBinding);
    }

    [Fact]
    public async Task AIFunctionFactoryOptions_SupportsSkippingParameters()
    {
        AIFunction func = AIFunctionFactory.Create(
            (string firstParameter, int secondParameter) => firstParameter + secondParameter,
            new()
            {
                ConfigureParameterBinding = p => p.Name == "firstParameter" ? new() { ExcludeFromSchema = true } : default,
            });

        Assert.DoesNotContain("firstParameter", func.JsonSchema.ToString());
        Assert.Contains("secondParameter", func.JsonSchema.ToString());

        Assert.Equal("""{"type":"string"}""", func.ReturnJsonSchema.ToString());

        var result = (JsonElement?)await func.InvokeAsync(new()
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

        Assert.Equal("""{"type":"integer"}""", func.ReturnJsonSchema.ToString());

        await Assert.ThrowsAsync<ArgumentNullException>("arguments.Services", () => func.InvokeAsync(arguments).AsTask());

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
    public async Task Create_NoInstance_UsesActivatorUtilitiesWhenServicesAvailable()
    {
        MyFunctionTypeWithOneArg mft = new(new());
        MyArgumentType mat = new();

        ServiceCollection sc = new();
        sc.AddSingleton(mft);
        sc.AddSingleton(mat);
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction func = AIFunctionFactory.Create(
            typeof(MyFunctionTypeWithOneArg).GetMethod(nameof(MyFunctionTypeWithOneArg.InstanceMethod))!,
            static arguments =>
            {
                Assert.NotNull(arguments.Services);
                return ActivatorUtilities.CreateInstance(arguments.Services, typeof(MyFunctionTypeWithOneArg));
            },
            new() { MarshalResult = (result, type, cancellationToken) => new ValueTask<object?>(result) });

        Assert.NotNull(func);
        var result = (Tuple<MyFunctionTypeWithOneArg, MyArgumentType>?)await func.InvokeAsync(new() { Services = sp });
        Assert.NotSame(mft, result?.Item1);
        Assert.Same(mat, result?.Item2);
    }

    [Fact]
    public async Task Create_CreateInstanceReturnsNull_ThrowsDuringInvocation()
    {
        AIFunction func = AIFunctionFactory.Create(
            typeof(MyFunctionTypeWithOneArg).GetMethod(nameof(MyFunctionTypeWithOneArg.InstanceMethod))!,
            static _ => null!);

        Assert.NotNull(func);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await func.InvokeAsync());
    }

    [Fact]
    public async Task Create_WrongConstructedType_ThrowsDuringInvocation()
    {
        AIFunction func = AIFunctionFactory.Create(
            typeof(MyFunctionTypeWithOneArg).GetMethod(nameof(MyFunctionTypeWithOneArg.InstanceMethod))!,
            static _ => new MyFunctionTypeWithNoArgs());

        Assert.NotNull(func);
        await Assert.ThrowsAsync<TargetException>(async () => await func.InvokeAsync());
    }

    [Fact]
    public void Create_NoInstance_ThrowsForStaticMethod()
    {
        Assert.Throws<ArgumentException>("method", () => AIFunctionFactory.Create(
            typeof(MyFunctionTypeWithNoArgs).GetMethod(nameof(MyFunctionTypeWithNoArgs.StaticMethod))!,
            static _ => new MyFunctionTypeWithNoArgs()));
    }

    [Fact]
    public async Task Create_NoInstance_DisposableInstanceCreatedDisposedEachInvocation()
    {
        AIFunction func = AIFunctionFactory.Create(
            typeof(DisposableService).GetMethod(nameof(DisposableService.GetThis))!,
            static _ => new DisposableService(),
            new()
            {
                MarshalResult = (result, type, cancellationToken) => new ValueTask<object?>(result),
            });

        var d1 = Assert.IsType<DisposableService>(await func.InvokeAsync());
        var d2 = Assert.IsType<DisposableService>(await func.InvokeAsync());
        Assert.NotSame(d1, d2);

        Assert.Equal(1, d1.Disposals);
        Assert.Equal(1, d2.Disposals);
    }

    [Fact]
    public async Task Create_NoInstance_AsyncDisposableInstanceCreatedDisposedEachInvocation()
    {
        AIFunction func = AIFunctionFactory.Create(
            typeof(AsyncDisposableService).GetMethod(nameof(AsyncDisposableService.GetThis))!,
            static _ => new AsyncDisposableService(),
            new()
            {
                MarshalResult = (result, type, cancellationToken) => new ValueTask<object?>(result),
            });

        var d1 = Assert.IsType<AsyncDisposableService>(await func.InvokeAsync());
        var d2 = Assert.IsType<AsyncDisposableService>(await func.InvokeAsync());
        Assert.NotSame(d1, d2);

        Assert.Equal(1, d1.AsyncDisposals);
        Assert.Equal(1, d2.AsyncDisposals);
    }

    [Fact]
    public async Task Create_NoInstance_DisposableAndAsyncDisposableInstanceCreatedDisposedEachInvocation()
    {
        AIFunction func = AIFunctionFactory.Create(
            typeof(DisposableAndAsyncDisposableService).GetMethod(nameof(DisposableAndAsyncDisposableService.GetThis))!,
            static _ => new DisposableAndAsyncDisposableService(),
            new()
            {
                MarshalResult = (result, type, cancellationToken) => new ValueTask<object?>(result),
            });

        var d1 = Assert.IsType<DisposableAndAsyncDisposableService>(await func.InvokeAsync());
        var d2 = Assert.IsType<DisposableAndAsyncDisposableService>(await func.InvokeAsync());
        Assert.NotSame(d1, d2);

        Assert.Equal(0, d1.Disposals);
        Assert.Equal(0, d2.Disposals);
        Assert.Equal(1, d1.AsyncDisposals);
        Assert.Equal(1, d2.AsyncDisposals);
    }

    [Fact]
    public async Task FromKeyedServices_ResolvesFromServiceProvider()
    {
        MyService service = new(42);

        ServiceCollection sc = new();
        sc.AddKeyedSingleton("key", service);
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(([FromKeyedServices("key")] MyService service, int myInteger) => service.Value + myInteger,
            CreateKeyedServicesSupportOptions());

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        Assert.Equal("""{"type":"integer"}""", f.ReturnJsonSchema.ToString());

        Exception e = await Assert.ThrowsAsync<ArgumentException>("arguments.Services", () => f.InvokeAsync(new() { ["myInteger"] = 1 }).AsTask());

        var result = await f.InvokeAsync(new() { ["myInteger"] = 1, Services = sp });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task FromKeyedServices_NullKeysBindToNonKeyedServices()
    {
        MyService service = new(42);

        ServiceCollection sc = new();
        sc.AddSingleton(service);
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(([FromKeyedServices(null!)] MyService service, int myInteger) => service.Value + myInteger,
            CreateKeyedServicesSupportOptions());

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        Assert.Equal("""{"type":"integer"}""", f.ReturnJsonSchema.ToString());

        Exception e = await Assert.ThrowsAsync<ArgumentException>("arguments.Services", () => f.InvokeAsync(new() { ["myInteger"] = 1 }).AsTask());

        var result = await f.InvokeAsync(new() { ["myInteger"] = 1, Services = sp });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task FromKeyedServices_OptionalDefaultsToNull()
    {
        MyService service = new(42);

        ServiceCollection sc = new();
        sc.AddKeyedSingleton("key", service);
        IServiceProvider sp = sc.BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(([FromKeyedServices("key")] MyService? service = null, int myInteger = 0) =>
            service is null ? "null " + 1 : (service.Value + myInteger).ToString(),
            CreateKeyedServicesSupportOptions());

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        var result = await f.InvokeAsync(new() { ["myInteger"] = 1 });
        Assert.Contains("null 1", result?.ToString());

        result = await f.InvokeAsync(new() { ["myInteger"] = 1, Services = sp });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task ConfigureParameterBinding_CanBeUsedToSupportFromContext()
    {
        MyService service = new(42);

        AIFunction f = AIFunctionFactory.Create(
            (MyService service, int myInteger) => service.Value + myInteger,
            new AIFunctionFactoryOptions
            {
                ConfigureParameterBinding = p =>
                {
                    if (p.ParameterType == typeof(MyService))
                    {
                        return new()
                        {
                            BindParameter = (p, a) =>
                                a.Context?.TryGetValue(typeof(MyService), out object? service) is true ? service :
                                p.HasDefaultValue ? p.DefaultValue :
                                throw new ArgumentException($"Unable to resolve argument for '{p.Name}'."),
                            ExcludeFromSchema = true
                        };
                    }

                    return default;
                }
            });

        Assert.Contains("myInteger", f.JsonSchema.ToString());
        Assert.DoesNotContain("service", f.JsonSchema.ToString());

        Exception e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync(new() { ["myInteger"] = 1 }).AsTask());
        Assert.Contains("Unable to resolve", e.Message);

        e = await Assert.ThrowsAsync<ArgumentException>(() => f.InvokeAsync(new()
        {
            ["myInteger"] = 1,
            Context = new Dictionary<object, object?>(),
        }).AsTask());
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
    public async Task ConfigureParameterBinding_CanBeUsedToOverrideServiceProvider()
    {
        IServiceProvider sp1 = new ServiceCollection().AddSingleton(new MyService(42)).BuildServiceProvider();
        IServiceProvider sp2 = new ServiceCollection().AddSingleton(new MyService(43)).BuildServiceProvider();

        AIFunction f = AIFunctionFactory.Create(
            (IServiceProvider services) => services.GetRequiredService<MyService>().Value,
            new AIFunctionFactoryOptions
            {
                ConfigureParameterBinding = p => new() { BindParameter = (p, a) => sp2 },
            });

        var result = await f.InvokeAsync(new() { Services = sp1 });
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task ConfigureParameterBinding_CanBeUsedToOverrideAIFunctionArguments()
    {
        AIFunctionArguments args1 = new() { ["a"] = 42 };
        AIFunctionArguments args2 = new() { ["a"] = 43 };

        AIFunction f = AIFunctionFactory.Create(
            (AIFunctionArguments args) => (int)args["a"]!,
            new AIFunctionFactoryOptions
            {
                ConfigureParameterBinding = p => new() { BindParameter = (p, a) => args2 },
            });

        var result = await f.InvokeAsync(args1);
        Assert.Contains("43", result?.ToString());
    }

    [Fact]
    public async Task MarshalResult_UsedForVoidReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            (int i) => { },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Null(result);
                    Assert.Null(type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_UsedForTaskReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async (int i) => { await Task.Yield(); },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Null(result);
                    Assert.Null(type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_UsedForValueTaskReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async ValueTask (int i) => { await Task.Yield(); },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Null(result);
                    Assert.Null(type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_UsedForTReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            (int i) => i,
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Equal(42, result);
                    Assert.Equal(typeof(int), type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_UsedForTaskTReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async (int i) => { await Task.Yield(); return i; },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Equal(42, result);
                    Assert.Equal(typeof(int), type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_UsedForValueTaskTReturningMethods()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async ValueTask<int> (int i) => { await Task.Yield(); return i; },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Equal(42, result);
                    Assert.Equal(typeof(int), type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_TypeIsDeclaredTypeEvenWhenNullReturned()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async ValueTask<string?> (int i) => { await Task.Yield(); return null; },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.Null(result);
                    Assert.Equal(typeof(string), type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task MarshalResult_TypeIsDeclaredTypeEvenWhenDerivedTypeReturned()
    {
        using CancellationTokenSource cts = new();

        AIFunction f = AIFunctionFactory.Create(
            async ValueTask<B> (int i) => { await Task.Yield(); return new C(); },
            new()
            {
                MarshalResult = async (result, type, cancellationToken) =>
                {
                    await Task.Yield();
                    Assert.IsType<C>(result);
                    Assert.Equal(typeof(B), type);
                    Assert.Equal(cts.Token, cancellationToken);
                    return "marshalResultInvoked";
                },
                SerializerOptions = JsonContext.Default.Options,
            });

        object? result = await f.InvokeAsync(new() { ["i"] = 42 }, cts.Token);
        Assert.Equal("marshalResultInvoked", result);
    }

    [Fact]
    public async Task AIFunctionFactory_DefaultDefaultParameter()
    {
        Assert.NotEqual(new StructWithDefaultCtor().Value, default(StructWithDefaultCtor).Value);

        AIFunction f = AIFunctionFactory.Create((Guid g = default, StructWithDefaultCtor s = default) => g.ToString() + "," + s.Value.ToString(), serializerOptions: JsonContext.Default.Options);

        object? result = await f.InvokeAsync();
        Assert.Contains("00000000-0000-0000-0000-000000000000,0", result?.ToString());
    }

    [Fact]
    public void AIFunctionFactory_ReturnTypeWithDescriptionAttribute()
    {
        AIFunction f = AIFunctionFactory.Create(Add, serializerOptions: JsonContext.Default.Options);

        Assert.Equal("""{"description":"The summed result","type":"integer"}""", f.ReturnJsonSchema.ToString());

        [return: Description("The summed result")]
        static int Add(int a, int b) => a + b;
    }

    private sealed class MyService(int value)
    {
        public int Value => value;
    }

    private class DisposableService : IDisposable
    {
        public int Disposals { get; private set; }
        public void Dispose() => Disposals++;

        public object GetThis() => this;
    }

    private class AsyncDisposableService : IAsyncDisposable
    {
        public int AsyncDisposals { get; private set; }

        public ValueTask DisposeAsync()
        {
            AsyncDisposals++;
            return default;
        }

        public object GetThis() => this;
    }

    private class DisposableAndAsyncDisposableService : IDisposable, IAsyncDisposable
    {
        public int Disposals { get; private set; }
        public int AsyncDisposals { get; private set; }

        public void Dispose() => Disposals++;

        public ValueTask DisposeAsync()
        {
            AsyncDisposals++;
            return default;
        }

        public object GetThis() => this;
    }

    private sealed class MyFunctionTypeWithNoArgs
    {
        public static void StaticMethod() => throw new NotSupportedException();
    }

    private sealed class MyFunctionTypeWithOneArg(MyArgumentType arg)
    {
        public object InstanceMethod() => Tuple.Create(this, arg);
    }

    private sealed class MyArgumentType;

    private class A;
    private class B : A;
    private sealed class C : B;

    public readonly struct StructWithDefaultCtor
    {
        public int Value { get; }
        public StructWithDefaultCtor()
        {
            Value = 42;
        }
    }

    private static AIFunctionFactoryOptions CreateKeyedServicesSupportOptions() =>
        new AIFunctionFactoryOptions
        {
            ConfigureParameterBinding = p =>
            {
                if (p.GetCustomAttribute<FromKeyedServicesAttribute>() is { } attr)
                {
                    return new()
                    {
                        BindParameter = (p, a) =>
                            (a.Services as IKeyedServiceProvider)?.GetKeyedService(p.ParameterType, attr.Key) is { } s ? s :
                            p.HasDefaultValue ? p.DefaultValue :
                            throw new ArgumentException($"Unable to resolve argument for '{p.Name}'.", "arguments.Services"),
                        ExcludeFromSchema = true
                    };
                }

                return default;
            },
        };

    [JsonSerializable(typeof(IAsyncEnumerable<int>))]
    [JsonSerializable(typeof(int[]))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(StructWithDefaultCtor))]
    [JsonSerializable(typeof(B))]
    private partial class JsonContext : JsonSerializerContext;
}
