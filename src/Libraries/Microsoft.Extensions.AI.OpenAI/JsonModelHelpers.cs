// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Text.Json;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines a set of helper methods for working with <see cref="IJsonModel{T}"/> types.
/// </summary>
internal static class JsonModelHelpers
{
    public static BinaryData Serialize<TModel>(TModel value)
        where TModel : IJsonModel<TModel>
    {
        return value.Write(ModelReaderWriterOptions.Json);
    }

    public static TModel Deserialize<TModel>(BinaryData data)
        where TModel : IJsonModel<TModel>, new()
    {
        return JsonModelDeserializationWitness<TModel>.Value.Create(data, ModelReaderWriterOptions.Json);
    }

    public static TModel Deserialize<TModel>(ref Utf8JsonReader reader)
        where TModel : IJsonModel<TModel>, new()
    {
        return JsonModelDeserializationWitness<TModel>.Value.Create(ref reader, ModelReaderWriterOptions.Json);
    }

    private sealed class JsonModelDeserializationWitness<TModel>
        where TModel : IJsonModel<TModel>, new()
    {
        public static readonly IJsonModel<TModel> Value = new TModel();
    }
}
