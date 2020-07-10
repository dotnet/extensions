// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    // It's possible to get this via ITextBufferFactoryService.InertContentType,
    // but plumbing it through is ugly and this can be used in unit tests as well
    internal class InertContentType : IContentType
    {
        public static readonly IContentType Instance = new InertContentType();

        public string TypeName => "inert";

        public string DisplayName => TypeName;

        public IEnumerable<IContentType> BaseTypes => Enumerable.Empty<IContentType>();

        public bool IsOfType(string type) => string.Equals(type, TypeName, StringComparison.OrdinalIgnoreCase);
    }
}
