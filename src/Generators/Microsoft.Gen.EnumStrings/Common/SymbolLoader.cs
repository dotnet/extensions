// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.EnumStrings;

internal static class SymbolLoader
{
    public const string EnumStringsAttribute = "Microsoft.Extensions.EnumStrings.EnumStringsAttribute";
    public const string FlagsAttribute = "System.FlagsAttribute";
    public const string FreezerClass = "System.Collections.Frozen.FrozenDictionary";

    public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
    {
        INamedTypeSymbol? GetSymbol(string metadataName, bool optional = false)
        {
            var symbol = compilation.GetTypeByMetadataName(metadataName);
            if (symbol == null && !optional)
            {
                return null;
            }

            return symbol;
        }

        // required
        var flagsAttributeSymbol = GetSymbol(FlagsAttribute);
        var enumStringsAttributeSymbol = GetSymbol(EnumStringsAttribute);

        if (flagsAttributeSymbol == null || enumStringsAttributeSymbol == null)
        {
            symbolHolder = default;
            return false;
        }

        symbolHolder = new(
            flagsAttributeSymbol,
            enumStringsAttributeSymbol,
            GetSymbol(FreezerClass));
        return true;
    }
}
