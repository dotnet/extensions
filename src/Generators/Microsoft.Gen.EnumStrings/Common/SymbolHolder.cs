// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.EnumStrings;

/// <summary>
/// Holds required symbols for the <see cref="Generator"/>.
/// </summary>
internal sealed record class SymbolHolder(
    INamedTypeSymbol FlagsAttributeSymbol,
    INamedTypeSymbol EnumStringsAttributeSymbol,
    INamedTypeSymbol? FreezerSymbol);
