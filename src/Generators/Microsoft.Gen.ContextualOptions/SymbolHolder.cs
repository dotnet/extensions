// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.ContextualOptions;

[ExcludeFromCodeCoverage]
internal sealed record class SymbolHolder(INamedTypeSymbol OptionsContextAttribute);
