// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This source file was lovingly 'borrowed' from dotnet/runtime/src/libraries/Microsoft.Extensions.Logging
#pragma warning disable S1128 // Unused "using" should be removed

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Logging
{
    internal static class ProviderAliasUtilities
    {
        private const string AliasAttributeTypeFullName = "Microsoft.Extensions.Logging.ProviderAliasAttribute";

        internal static string? GetAlias(Type providerType)
        {
            IList<CustomAttributeData> attributes = CustomAttributeData.GetCustomAttributes(providerType);

            for (int i = 0; i < attributes.Count; i++)
            {
                CustomAttributeData attributeData = attributes[i];
                if (attributeData.AttributeType.FullName == AliasAttributeTypeFullName &&
                    attributeData.ConstructorArguments.Count > 0)
                {
                    CustomAttributeTypedArgument arg = attributeData.ConstructorArguments[0];

                    return arg.Value?.ToString();
                }
            }

            return null;
        }
    }
}
