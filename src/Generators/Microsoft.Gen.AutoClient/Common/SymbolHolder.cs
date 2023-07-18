// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.AutoClient;

internal sealed record class SymbolHolder(
    INamedTypeSymbol RestApiAttribute,
    INamedTypeSymbol? RestGetAttribute,
    INamedTypeSymbol? RestPostAttribute,
    INamedTypeSymbol? RestPutAttribute,
    INamedTypeSymbol? RestDeleteAttribute,
    INamedTypeSymbol? RestPatchAttribute,
    INamedTypeSymbol? RestOptionsAttribute,
    INamedTypeSymbol? RestHeadAttribute,
    INamedTypeSymbol? RestStaticHeaderAttribute,
    INamedTypeSymbol? RestHeaderAttribute,
    INamedTypeSymbol? RestQueryAttribute,
    INamedTypeSymbol? RestBodyAttribute);
