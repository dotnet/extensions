// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

[JsonSerializable(typeof(DistributedCachingChatClientTest.CustomAIContent1))]
[JsonSerializable(typeof(DistributedCachingChatClientTest.CustomAIContent2))]
internal sealed partial class CustomAIContentJsonContext : JsonSerializerContext;
