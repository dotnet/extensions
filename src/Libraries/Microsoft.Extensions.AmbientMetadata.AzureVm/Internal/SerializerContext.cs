// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AmbientMetadata.Internal;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(AzureVmMetadata))]
internal sealed partial class SerializerContext : JsonSerializerContext
{
}
