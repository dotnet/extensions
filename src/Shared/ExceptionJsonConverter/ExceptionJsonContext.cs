// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

#pragma warning disable CA1716
namespace Microsoft.Shared.ExceptionJsonConverter;
#pragma warning restore CA1716

[JsonSerializable(typeof(Exception))]
[JsonSourceGenerationOptions(Converters = new Type[] { typeof(ExceptionJsonConverter) })]
internal sealed partial class ExceptionJsonContext : JsonSerializerContext
{
}
