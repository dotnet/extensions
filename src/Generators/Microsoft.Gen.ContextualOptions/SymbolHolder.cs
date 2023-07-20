// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.ContextualOptions;

internal sealed record class SymbolHolder(INamedTypeSymbol OptionsContextAttribute);
