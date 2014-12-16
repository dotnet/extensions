// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.AspNet.Testing.xunit
{
    internal class SkipReasonAttributeInfo : IAttributeInfo
    {
        private readonly IAttributeInfo _wrappedAttribute;
        private readonly string _skipReason;

        public SkipReasonAttributeInfo(string skipReason, IAttributeInfo wrappedAttribute)
        {
            _wrappedAttribute = wrappedAttribute;
            _skipReason = skipReason;
        }

        public TValue GetNamedArgument<TValue>(string argumentName)
        {
            var argumentValue = _wrappedAttribute.GetNamedArgument<TValue>(argumentName);

            // Override the skip reason if we have one and there 
            // was not already one specified by the user
            if (_skipReason != null &&
                typeof(TValue) == typeof(string) &&
                argumentName == "Skip")
            {
                string stringValue = (string)(object)argumentValue;
                if (stringValue == null)
                {
                    return (TValue)(object)_skipReason;
                }
            }

            return argumentValue;
        }

        public IEnumerable<object> GetConstructorArguments()
        {
            return _wrappedAttribute.GetConstructorArguments();
        }

        public IEnumerable<IAttributeInfo> GetCustomAttributes(string assemblyQualifiedAttributeTypeName)
        {
            return _wrappedAttribute.GetCustomAttributes(assemblyQualifiedAttributeTypeName);
        }
    }
}