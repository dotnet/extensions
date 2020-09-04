// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    public class HtmlFormattingTest : FormattingTestBase
    {
        [Fact]
        public async Task FormatsSimpleHtmlTag()
        {
            await RunFormattingTestAsync(
input: @"
|   <div>
</div>|
",
expected: @"
<div>
</div>
");
        }

        [Fact]
        public async Task FormatsRazorHtmlBlock()
        {
            await RunFormattingTestAsync(
input: @"|@page ""/error""

        <h1 class=
""text-danger"">Error.</h1>
    <h2 class=""text-danger"">An error occurred while processing your request.</h2>

            <h3>Development Mode</h3>
<p>
    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.</p>
<p>
    <strong>The Development environment shouldn't be enabled for deployed applications.
</strong>
</p>
|",
expected: @"@page ""/error""

<h1 class=""text-danger"">
    Error.
</h1>
<h2 class=""text-danger"">An error occurred while processing your request.</h2>

<h3>Development Mode</h3>
<p>
    Swapping to <strong>Development</strong> environment will display more detailed information about the error that occurred.
</p>
<p>
    <strong>
        The Development environment shouldn't be enabled for deployed applications.
    </strong>
</p>
");
        }

        [Fact]
        public async Task FormatsMixedHtmlBlock()
        {
            await RunFormattingTestAsync(
input: @"|@page ""/test""
@{
<p>
        @{
                var t = 1;
if (true)
{

            }
        }
        </p>
}
|",
expected: @"@page ""/test""
@{
    <p>
        @{
            var t = 1;
            if (true)
            {

            }
        }
    </p>
}
");
        }
    }
}
