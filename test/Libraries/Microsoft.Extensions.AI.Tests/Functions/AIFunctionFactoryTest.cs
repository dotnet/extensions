// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.AI;

public class AIFunctionFactoryTest
{
    [Fact]
    public void InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => AIFunctionFactory.Create(method: null!));
        Assert.Throws<ArgumentNullException>(() => AIFunctionFactory.Create(method: null!, target: new object()));
        Assert.Throws<ArgumentNullException>(() => AIFunctionFactory.Create(method: null!, target: new object(), name: "myAiFunk"));
        Assert.Throws<ArgumentNullException>(() => AIFunctionFactory.Create(typeof(AIFunctionFactoryTest).GetMethod(nameof(InvalidArguments_Throw))!, null));
        Assert.Throws<ArgumentException>(() => AIFunctionFactory.Create(typeof(List<>).GetMethod("Add")!, new List<int>()));
    }

    [Fact]
    public async Task Parameters_MappedByName_Async()
    {
        AIFunction func;

        func = AIFunctionFactory.Create((string a) => a + " " + a);
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync([new KeyValuePair<string, object?>("a", "test")]));

        func = AIFunctionFactory.Create((string a, string b) => b + " " + a);
        AssertExtensions.EqualFunctionCallResults("hello world", await func.InvokeAsync([new KeyValuePair<string, object?>("b", "hello"), new KeyValuePair<string, object?>("a", "world")]));

        func = AIFunctionFactory.Create((int a, long b) => a + b);
        AssertExtensions.EqualFunctionCallResults(3L, await func.InvokeAsync([new KeyValuePair<string, object?>("a", 1), new KeyValuePair<string, object?>("b", 2L)]));
    }

    [Fact]
    public async Task Parameters_DefaultValuesAreUsedButOverridable_Async()
    {
        AIFunction func = AIFunctionFactory.Create((string a = "test") => a + " " + a);
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync());
        AssertExtensions.EqualFunctionCallResults("hello hello", await func.InvokeAsync([new KeyValuePair<string, object?>("a", "hello")]));
    }

    [Fact]
    public async Task Parameters_AIFunctionContextMappedByType_Async()
    {
        using var cts = new CancellationTokenSource();
        CancellationToken written;
        AIFunction func;

        // As the only parameter
        written = default;
        func = AIFunctionFactory.Create((AIFunctionContext ctx) =>
        {
            Assert.NotNull(ctx);
            written = ctx.CancellationToken;
        });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(cancellationToken: cts.Token));
        Assert.Equal(cts.Token, written);

        // As the last
        written = default;
        func = AIFunctionFactory.Create((int somethingFirst, AIFunctionContext ctx) =>
        {
            Assert.NotNull(ctx);
            written = ctx.CancellationToken;
        });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(new Dictionary<string, object?> { ["somethingFirst"] = 1, ["ctx"] = new AIFunctionContext() }, cts.Token));
        Assert.Equal(cts.Token, written);

        // As the first
        written = default;
        func = AIFunctionFactory.Create((AIFunctionContext ctx, int somethingAfter = 0) =>
        {
            Assert.NotNull(ctx);
            written = ctx.CancellationToken;
        });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync(cancellationToken: cts.Token));
        Assert.Equal(cts.Token, written);
    }

    [Fact]
    public async Task Returns_AsyncReturnTypesSupported_Async()
    {
        AIFunction func;

        func = AIFunctionFactory.Create(Task<string> (string a) => Task.FromResult(a + " " + a));
        AssertExtensions.EqualFunctionCallResults("test test", await func.InvokeAsync([new KeyValuePair<string, object?>("a", "test")]));

        func = AIFunctionFactory.Create(ValueTask<string> (string a, string b) => new ValueTask<string>(b + " " + a));
        AssertExtensions.EqualFunctionCallResults("hello world", await func.InvokeAsync([new KeyValuePair<string, object?>("b", "hello"), new KeyValuePair<string, object?>("a", "world")]));

        long result = 0;
        func = AIFunctionFactory.Create(async Task (int a, long b) => { result = a + b; await Task.Yield(); });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync([new KeyValuePair<string, object?>("a", 1), new KeyValuePair<string, object?>("b", 2L)]));
        Assert.Equal(3, result);

        result = 0;
        func = AIFunctionFactory.Create(async ValueTask (int a, long b) => { result = a + b; await Task.Yield(); });
        AssertExtensions.EqualFunctionCallResults(null, await func.InvokeAsync([new KeyValuePair<string, object?>("a", 1), new KeyValuePair<string, object?>("b", 2L)]));
        Assert.Equal(3, result);

        func = AIFunctionFactory.Create((int count) => SimpleIAsyncEnumerable(count));
        AssertExtensions.EqualFunctionCallResults(new int[] { 0, 1, 2, 3, 4 }, await func.InvokeAsync([new("count", 5)]));

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

        func = AIFunctionFactory.Create(() => "test");
        Assert.Contains("Metadata_DerivedFromLambda", func.Metadata.Name);
        Assert.Empty(func.Metadata.Description);
        Assert.Empty(func.Metadata.Parameters);
        Assert.Equal(typeof(string), func.Metadata.ReturnParameter.ParameterType);

        func = AIFunctionFactory.Create((string a) => a + " " + a);
        Assert.Contains("Metadata_DerivedFromLambda", func.Metadata.Name);
        Assert.Empty(func.Metadata.Description);
        Assert.Single(func.Metadata.Parameters);

        func = AIFunctionFactory.Create(
            [Description("This is a test function")] ([Description("This is A")] string a, [Description("This is B")] string b) => b + " " + a);
        Assert.Contains("Metadata_DerivedFromLambda", func.Metadata.Name);
        Assert.Equal("This is a test function", func.Metadata.Description);
        Assert.Collection(func.Metadata.Parameters,
            p => Assert.Equal("This is A", p.Description),
            p => Assert.Equal("This is B", p.Description));
    }

    [Fact]
    public void AIFunctionFactoryCreateOptions_ValuesPropagateToAIFunction()
    {
        IReadOnlyList<AIFunctionParameterMetadata> parameterMetadata = [new AIFunctionParameterMetadata("a")];
        AIFunctionReturnParameterMetadata returnParameterMetadata = new() { ParameterType = typeof(string) };
        IReadOnlyDictionary<string, object?> metadata = new Dictionary<string, object?> { ["a"] = "b" };

        var options = new AIFunctionFactoryCreateOptions
        {
            Name = "test name",
            Description = "test description",
            Parameters = parameterMetadata,
            ReturnParameter = returnParameterMetadata,
            AdditionalProperties = metadata,
        };

        Assert.Equal("test name", options.Name);
        Assert.Equal("test description", options.Description);
        Assert.Same(parameterMetadata, options.Parameters);
        Assert.Same(returnParameterMetadata, options.ReturnParameter);
        Assert.Same(metadata, options.AdditionalProperties);

        AIFunction func = AIFunctionFactory.Create(() => { }, options);

        Assert.Equal("test name", func.Metadata.Name);
        Assert.Equal("test description", func.Metadata.Description);
        Assert.Equal(parameterMetadata, func.Metadata.Parameters);
        Assert.Equal(returnParameterMetadata, func.Metadata.ReturnParameter);
        Assert.Equal(metadata, func.Metadata.AdditionalProperties);
    }
}
