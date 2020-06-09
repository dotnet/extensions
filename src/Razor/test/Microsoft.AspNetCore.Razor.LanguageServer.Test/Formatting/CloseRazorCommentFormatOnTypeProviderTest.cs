// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class CloseRazorCommentFormatOnTypeProviderTest : FormatOnTypeProviderTestBase
    {
        [Fact]
        public void OnTypeStar_ClosesRazorComment()
        {
            RunFormatOnTypeTest(
input: @"
@|
",
expected: $@"
@* {LanguageServerConstants.CursorPlaceholderString} *@
",
character: "*");
        }

        [Fact]
        public void OnTypeStar_InsideRazorComment_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@* @| *@
",
expected: $@"
@* @* *@
",
character: "*");
        }

        [Fact]
        public void OnTypeStar_EndRazorComment_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@* Hello |@
",
expected: $@"
@* Hello *@
",
character: "*");
        }

        [Fact]
        public void OnTypeStar_BeforeText_ClosesRazorComment()
        {
            RunFormatOnTypeTest(
input: @"
@| Hello
",
expected: $@"
@* {LanguageServerConstants.CursorPlaceholderString} *@ Hello
",
character: "*");
        }

        internal override RazorFormatOnTypeProvider CreateProvider()
        {
            var provider = new CloseRazorCommentFormatOnTypeProvider();
            return provider;
        }
    }
}
