// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.ContextualOptions;

internal static class SymbolLoader
{
    public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
    {
        symbolHolder = default;

        var optionsContextAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.Options.Contextual.OptionsContextAttribute");
        if (optionsContextAttribute is null)
        {
            return false;
        }

        symbolHolder = new SymbolHolder(optionsContextAttribute);
        return true;
    }
}
