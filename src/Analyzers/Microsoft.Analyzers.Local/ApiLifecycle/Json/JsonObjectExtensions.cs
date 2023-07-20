// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.Json;

namespace Microsoft.Extensions.LocalAnalyzers.Json;

internal static class JsonObjectExtensions
{
    public static T[] GetValueArray<T>(this JsonObject value, string name)
    {
        var arrayOfTypes = value[name].AsJsonArray;

        if (arrayOfTypes == null)
        {
            return Array.Empty<T>();
        }

        var types = new T[arrayOfTypes.Count];

        for (var i = 0; i < arrayOfTypes.Count; i++)
        {
            types[i] = (T)Activator.CreateInstance(typeof(T), arrayOfTypes[i].AsJsonObject);
        }

        return types;
    }
}
