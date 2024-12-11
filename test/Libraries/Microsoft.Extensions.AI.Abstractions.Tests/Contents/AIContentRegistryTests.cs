// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Microsoft.Extensions.AI.Contents;

public static partial class AIContentRegistryTests
{
    [Fact]
    public static void DerivedAIContent_SerializeUsingRegistry()
    {
        JsonSerializerOptions options = AIJsonUtilities.DefaultOptions;

        AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent>("derivativeContent", DerivedAIContentContext.Default);
        AIContent c = new DerivedAIContent();

        JsonElement expectedJson = JsonDocument.Parse("""{"$type":"derivativeContent"}""").RootElement;
        JsonElement json = JsonSerializer.SerializeToElement(c, options);
        Assert.True(JsonElement.DeepEquals(expectedJson, json));

        AIContent? deserialized = JsonSerializer.Deserialize<AIContent>(json, options);
        Assert.IsType<DerivedAIContent>(deserialized);
    }

    private sealed class DerivedAIContent : AIContent;

    [JsonSerializable(typeof(DerivedAIContent))]
    private partial class DerivedAIContentContext : JsonSerializerContext;

    [Fact]
    public static void RegisterCustomAIContentType_NonAIContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AIContentRegistry.RegisterCustomAIContentType(typeof(int), "discriminator"));
        Assert.Throws<ArgumentException>(() => AIContentRegistry.RegisterCustomAIContentType(typeof(object), "discriminator"));
        Assert.Throws<ArgumentException>(() => AIContentRegistry.RegisterCustomAIContentType(typeof(ChatMessage), "discriminator"));
    }

    [Fact]
    public static void RegisterCustomAIContentType_BuildInAIContent_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => AIContentRegistry.RegisterCustomAIContentType<AIContent>("discriminator"));
        Assert.Throws<ArgumentException>(() => AIContentRegistry.RegisterCustomAIContentType<TextContent>("discriminator"));
    }

    [Fact]
    public static void RegisterCustomAIContentType_ConflictingIdentifier_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent2>("text"));
        Assert.Throws<InvalidOperationException>(() => AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent2>("audio"));

        AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent2>("discriminator");
        AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent2>("discriminator"); // Matching configurations are idempotent.
        Assert.Throws<InvalidOperationException>(() => AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent2>("discriminator2"));
    }

    private sealed class DerivedAIContent2 : AIContent;

    [Fact]
    public static void NullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AIContentRegistry.RegisterCustomAIContentType<DerivedAIContent3>(null!));
        Assert.Throws<ArgumentNullException>(() => AIContentRegistry.RegisterCustomAIContentType(typeof(DerivedAIContent3), null!));
        Assert.Throws<ArgumentNullException>(() => AIContentRegistry.RegisterCustomAIContentType(null!, "discriminator"));
        Assert.Throws<ArgumentNullException>(() => AIContentRegistry.ApplyAIContentRegistry(null!));
    }

    private sealed class DerivedAIContent3 : AIContent;
}
