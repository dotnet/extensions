// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.EnumStrings;

internal static class SymbolLoader
{
    public const string EnumStringsAttribute = "Microsoft.Extensions.EnumStrings.EnumStringsAttribute";
    public const string FlagsAttribute = "System.FlagsAttribute";
    public const string FreezerClass = "System.Collections.Frozen.FrozenDictionary";

    public static void Load(Compilation compilation, out SymbolHolder? symbolHolder)
    {
        symbolHolder = new(
            compilation.GetTypeByMetadataName(FlagsAttribute)!,
            compilation.GetTypeByMetadataName(EnumStringsAttribute)!,
            compilation.GetTypeByMetadataName(FreezerClass));
    }
}
