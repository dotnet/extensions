// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.BuildMetadata;

internal readonly record struct BuildMetadata(string? BuildId, string? BuildNumber, string? SourceBranchName, string? SourceVersion);
