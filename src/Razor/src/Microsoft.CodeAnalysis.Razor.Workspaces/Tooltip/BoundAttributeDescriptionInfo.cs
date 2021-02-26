// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Tooltip
{
    internal sealed class BoundAttributeDescriptionInfo : IEquatable<BoundAttributeDescriptionInfo>
    {
        public BoundAttributeDescriptionInfo(
            string returnTypeName,
            string typeName,
            string propertyName,
            string documentation)
        {
            if (returnTypeName is null)
            {
                throw new ArgumentNullException(nameof(returnTypeName));
            }

            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (propertyName is null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            ReturnTypeName = returnTypeName;
            TypeName = typeName;
            PropertyName = propertyName;
            Documentation = documentation ?? string.Empty;
        }

        public string ReturnTypeName { get; }

        public string TypeName { get; }

        public string PropertyName { get; }

        public string Documentation { get; }

        public bool Equals(BoundAttributeDescriptionInfo other)
        {
            if (!string.Equals(ReturnTypeName, other.ReturnTypeName, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(TypeName, other.TypeName, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.Equals(Documentation, other.Documentation, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            var combiner = HashCodeCombiner.Start();
            combiner.Add(ReturnTypeName);
            combiner.Add(TypeName);
            combiner.Add(PropertyName);
            combiner.Add(Documentation);

            return combiner.CombinedHash;
        }

        public static BoundAttributeDescriptionInfo From(BoundAttributeParameterDescriptor parameterAttribute, string parentTagHelperTypeName)
        {
            if (parameterAttribute is null)
            {
                throw new ArgumentNullException(nameof(parameterAttribute));
            }

            if (parentTagHelperTypeName is null)
            {
                throw new ArgumentNullException(nameof(parentTagHelperTypeName));
            }

            var propertyName = parameterAttribute.GetPropertyName();
            var descriptionInfo = new BoundAttributeDescriptionInfo(
                parameterAttribute.TypeName,
                parentTagHelperTypeName,
                propertyName,
                parameterAttribute.Documentation);
            return descriptionInfo;
        }

        public static BoundAttributeDescriptionInfo From(BoundAttributeDescriptor boundAttribute, bool indexer) => From(boundAttribute, indexer, parentTagHelperTypeName: null);

        public static BoundAttributeDescriptionInfo From(BoundAttributeDescriptor boundAttribute, bool indexer, string? parentTagHelperTypeName)
        {
            if (boundAttribute is null)
            {
                throw new ArgumentNullException(nameof(boundAttribute));
            }

            var returnTypeName = indexer ? boundAttribute.IndexerTypeName : boundAttribute.TypeName;
            var propertyName = boundAttribute.GetPropertyName();
            if (parentTagHelperTypeName == null)
            {

                // The BoundAttributeDescriptor does not directly have the TagHelperTypeName information available. Because of this we need to resolve it from other parts of it.
                parentTagHelperTypeName = ResolveTagHelperTypeName(returnTypeName, propertyName, boundAttribute.DisplayName);
            }

            var descriptionInfo = new BoundAttributeDescriptionInfo(
                returnTypeName,
                parentTagHelperTypeName,
                propertyName,
                boundAttribute.Documentation);
            return descriptionInfo;
        }

        // Internal for testing
        internal static string ResolveTagHelperTypeName(string returnTypeName, string propertyName, string displayName)
        {
            // A BoundAttributeDescriptor does not have a direct reference to its parent TagHelper.
            // However, when it was constructed the parent TagHelper's type name was embedded into
            // its DisplayName. In VSCode we can't use the DisplayName verbatim for descriptions
            // because the DisplayName is typically too long to display properly. Therefore we need
            // to break it apart and then reconstruct it in a reduced format.
            // i.e. this is the format the display name comes in:
            // ReturnTypeName SomeTypeName.SomePropertyName

            // We must simplify the return type name before using it to determine the type name prefix
            // because that is how the display name was originally built (a little hacky).
            if (!TypeNameStringResolver.TryGetSimpleName(returnTypeName, out var simpleReturnType))
            {
                simpleReturnType = returnTypeName;
            }

            // "ReturnTypeName "
            var typeNamePrefixLength = simpleReturnType.Length + 1 /* space */;

            // ".SomePropertyName"
            var typeNameSuffixLength = /* . */ 1 + propertyName.Length;

            // "SomeTypeName"
            var typeNameLength = displayName.Length - typeNamePrefixLength - typeNameSuffixLength;
            var tagHelperTypeName = displayName.Substring(typeNamePrefixLength, typeNameLength);
            return tagHelperTypeName;
        }
    }
}
