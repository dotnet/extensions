// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Microsoft.Extensions.AI.JsonSchemaExporter;

internal sealed record TestData<T>(
    T? Value,
    [StringSyntax(StringSyntaxAttribute.Json)] string ExpectedJsonSchema,
    IEnumerable<T?>? AdditionalValues = null,
    JsonSchemaExporterOptions? ExporterOptions = null,
    JsonSerializerOptions? Options = null,
    bool WritesNumbersAsStrings = false)
    : ITestData
{
    private static readonly JsonDocumentOptions _schemaParseOptions = new() { CommentHandling = JsonCommentHandling.Skip };

    public Type Type => typeof(T);
    object? ITestData.Value => Value;
    object? ITestData.ExporterOptions => ExporterOptions;
    JsonNode ITestData.ExpectedJsonSchema { get; } =
        JsonNode.Parse(ExpectedJsonSchema, documentOptions: _schemaParseOptions)
        ?? throw new ArgumentNullException("schema must not be null");

    IEnumerable<ITestData> ITestData.GetTestDataForAllValues()
    {
        yield return this;

        if (default(T) is null &&
            ExporterOptions is { TreatNullObliviousAsNonNullable: false } &&
            Value is not null)
        {
            yield return this with { Value = default };
        }

        if (AdditionalValues != null)
        {
            foreach (T? value in AdditionalValues)
            {
                yield return this with { Value = value, AdditionalValues = null };
            }
        }
    }
}

public interface ITestData
{
    Type Type { get; }

    object? Value { get; }

    JsonNode ExpectedJsonSchema { get; }

    object? ExporterOptions { get; }

    JsonSerializerOptions? Options { get; }

    bool WritesNumbersAsStrings { get; }

    IEnumerable<ITestData> GetTestDataForAllValues();
}
