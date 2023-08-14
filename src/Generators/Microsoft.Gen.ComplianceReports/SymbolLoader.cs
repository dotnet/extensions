// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Gen.ComplianceReports;

internal static class SymbolLoader
{
    private const string DataClassificationAttribute = "Microsoft.Extensions.Compliance.Classification.DataClassificationAttribute";
    private const string LoggerMessageAttribute = "Microsoft.Extensions.Logging.LoggerMessageAttribute";

    public static bool TryLoad(Compilation compilation, out SymbolHolder? symbolHolder)
    {
        // required
        var dataClassificationAttributeSymbol = compilation.GetTypeByMetadataName(DataClassificationAttribute);

        if (dataClassificationAttributeSymbol == null)
        {
            symbolHolder = default;
            return false;
        }

        symbolHolder = new(
            dataClassificationAttributeSymbol,
            compilation.GetTypeByMetadataName(LoggerMessageAttribute));

        return true;
    }
}
