// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.Extensions.AI.JsonSchemaExporter;

internal sealed record TestData<T>(
    T? Value,
    [StringSyntax(StringSyntaxAttribute.Json)] string ExpectedJsonSchema,
    IEnumerable<T?>? AdditionalValues = null,
#if TESTS_JSON_SCHEMA_EXPORTER_POLYFILL
    System.Text.Json.Schema.JsonSchemaExporterOptions? ExporterOptions = null,
#endif
    JsonSerializerOptions? Options = null,
    bool WritesNumbersAsStrings = false)
    : ITestData
{
    private static readonly JsonDocumentOptions _schemaParseOptions = new() { CommentHandling = JsonCommentHandling.Skip };

    public Type Type => typeof(T);
    object? ITestData.Value => Value;
#if TESTS_JSON_SCHEMA_EXPORTER_POLYFILL
    object? ITestData.ExporterOptions => ExporterOptions;
#endif
    JsonNode ITestData.ExpectedJsonSchema { get; } =
        JsonNode.Parse(ExpectedJsonSchema, documentOptions: _schemaParseOptions)
        ?? throw new ArgumentNullException("schema must not be null");

    IEnumerable<ITestData> ITestData.GetTestDataForAllValues()
    {
        yield return this;

        if (default(T) is null &&
#if TESTS_JSON_SCHEMA_EXPORTER_POLYFILL
            ExporterOptions is System.Text.Json.Schema.JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable: false } &&
#endif
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

#if TESTS_JSON_SCHEMA_EXPORTER_POLYFILL
    object? ExporterOptions { get; }
#endif

    JsonSerializerOptions? Options { get; }

    bool WritesNumbersAsStrings { get; }

    IEnumerable<ITestData> GetTestDataForAllValues();
}
