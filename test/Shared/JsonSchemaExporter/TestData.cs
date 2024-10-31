// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Schema;

namespace Microsoft.Extensions.AI.JsonSchemaExporter;

internal sealed record TestData<T>(
    T? Value,
    IEnumerable<T?>? AdditionalValues = null,
    [StringSyntax("Json")] string? ExpectedJsonSchema = null,
    JsonSchemaExporterOptions? ExporterOptions = null,
    JsonSerializerOptions? Options = null)
    : ITestData
{
    public Type Type => typeof(T);
    object? ITestData.Value => Value;
    object? ITestData.ExporterOptions => ExporterOptions;

    IEnumerable<ITestData> ITestData.GetTestDataForAllValues()
    {
        yield return this;

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

    /// <summary>
    /// Gets the expected JSON schema for the value.
    /// Fall back to JsonSchemaGenerator as the source of truth if null.
    /// </summary>
    string? ExpectedJsonSchema { get; }

    object? ExporterOptions { get; }

    JsonSerializerOptions? Options { get; }

    IEnumerable<ITestData> GetTestDataForAllValues();
}
