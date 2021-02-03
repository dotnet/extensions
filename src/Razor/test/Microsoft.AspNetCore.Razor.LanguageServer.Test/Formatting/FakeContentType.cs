// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    internal class FakeContentType : IContentType
    {
        public FakeContentType(string typeName, ImmutableArray<IContentType> baseTypes)
        {
            Debug.Assert(!string.IsNullOrEmpty(typeName));
            Debug.Assert(!baseTypes.IsDefault);

            TypeName = typeName;
            BaseTypes = baseTypes;
        }

        public string TypeName { get; }

        public string DisplayName => throw new NotImplementedException();

        public IEnumerable<IContentType> BaseTypes { get; }

        public bool IsOfType(string type)
        {
            return TypeName == type
                || BaseTypes.Any(baseType => baseType.IsOfType(type));
        }
    }
}
