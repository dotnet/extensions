// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public class CloseRazorCommentOnAutoInsertProviderTest : RazorOnAutoInsertProviderTestBase
    {
        [Fact]
        public void OnTypeStar_ClosesRazorComment()
        {
            RunAutoInsertTest(
input: @"
@|
",
expected: @"
@* $0 *@
",
character: "*");
        }

        [Fact]
        public void OnTypeStar_InsideRazorComment_Noops()
        {
            RunAutoInsertTest(
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
            RunAutoInsertTest(
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
            RunAutoInsertTest(
input: @"
@| Hello
",
expected: @"
@* $0 *@ Hello
",
character: "*");
        }

        internal override RazorOnAutoInsertProvider CreateProvider()
        {
            var provider = new CloseRazorCommentOnAutoInsertProvider();
            return provider;
        }
    }
}
