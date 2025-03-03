// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Schema;
using Xunit;

namespace Microsoft.Extensions.AI.JsonSchemaExporter;

public static class JsonSchemaExporterConfigurationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void JsonSchemaExporterOptions_DefaultValues(bool useSingleton)
    {
        JsonSchemaExporterOptions configuration = useSingleton ? JsonSchemaExporterOptions.Default : new();
        Assert.False(configuration.TreatNullObliviousAsNonNullable);
        Assert.Null(configuration.TransformSchemaNode);
    }

    [Fact]
    public static void JsonSchemaExporterOptions_Singleton_ReturnsSameInstance()
    {
        Assert.Same(JsonSchemaExporterOptions.Default, JsonSchemaExporterOptions.Default);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void JsonSchemaExporterOptions_TreatNullObliviousAsNonNullable(bool treatNullObliviousAsNonNullable)
    {
        JsonSchemaExporterOptions configuration = new() { TreatNullObliviousAsNonNullable = treatNullObliviousAsNonNullable };
        Assert.Equal(treatNullObliviousAsNonNullable, configuration.TreatNullObliviousAsNonNullable);
    }
}
