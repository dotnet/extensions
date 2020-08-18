// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    // Used to indicate that no other entity should respond to the edit event associated with this tag.
    internal class InviolableEditTag : IInviolableEditTag
    {
        private InviolableEditTag() { }

        public readonly static IInviolableEditTag Instance = new InviolableEditTag();
    }
}
