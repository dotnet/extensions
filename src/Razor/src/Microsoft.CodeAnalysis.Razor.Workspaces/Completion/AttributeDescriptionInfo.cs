// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Completion
{
    internal sealed class AttributeDescriptionInfo : IEquatable<AttributeDescriptionInfo>
    {
        public AttributeDescriptionInfo(
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

        public bool Equals(AttributeDescriptionInfo other)
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
    }
}