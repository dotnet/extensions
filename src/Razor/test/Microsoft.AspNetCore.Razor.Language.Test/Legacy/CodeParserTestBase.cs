// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public abstract class CodeParserTestBase : ParserTestBase
    {
        internal virtual ISet<string> KeywordSet
        {
            get { return CSharpCodeParser.DefaultKeywords; }
        }

        internal override RazorSyntaxTree ParseBlock(
            RazorLanguageVersion version, 
            string document, 
            IEnumerable<DirectiveDescriptor> directives, 
            bool designTime)
        {
            return ParseCodeBlock(version, document, directives, designTime);
        }
    }
}
