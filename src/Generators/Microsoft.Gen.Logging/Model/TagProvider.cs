// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Gen.Logging.Model;

[ExcludeFromCodeCoverage]
internal sealed record class TagProvider(
    string MethodName,
    string ContainingType);
