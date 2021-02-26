// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    internal interface IInterceptMethodMetadata
    {
        // this must match the name from InterceptMethodAttribute
        IEnumerable<string> InterceptMethods { get; }
    }
}
