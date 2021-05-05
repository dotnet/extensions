// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal abstract class FallbackCapabilitiesFilterResolver
    {
        public abstract Func<JToken, bool> Resolve(string lspRequestMethodName);
    }
}
