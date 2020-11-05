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
|   <html>
<head>
   <title>Hello</title></head>
<body><div>
</div>
        </body>
 </html>|
",
expected: @"
<html>
<head>
    <title>Hello</title>
</head>
<body>
    <div>
    </div>
</body>
</html>
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
            <div>
 <div>
    <div>
<div>
        This is heavily nested
</div>
 </div>
    </div>
        </div>
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
    <div>
        <div>
            <div>
                <div>
                    This is heavily nested
                </div>
            </div>
        </div>
    </div>
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
<div>
 @{
    <div>
<div>
        This is heavily nested
</div>
 </div>
    }
        </div>
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
    <div>
        @{
            <div>
                <div>
                    This is heavily nested
                </div>
            </div>
        }
    </div>
}
");
        }

        [Fact]
        public async Task FormatsMixedRazorBlock()
        {
            await RunFormattingTestAsync(
input: @"|@page ""/test""

<div class=@className>Some Text</div>

@{
<p>
        @if (true) {
                var t = 1;
if (true)
{
<div>@DateTime.Now</div>
            }
        }
        </p>
}
|",
expected: @"@page ""/test""

<div class=@className>Some Text</div>

@{
    <p>
        @if (true)
        {
            var t = 1;
            if (true)
            {
                <div>@DateTime.Now</div>
            }
        }
    </p>
}
");
        }

        [Fact]
        public async Task FormatsComplexBlock()
        {
            await RunFormattingTestAsync(
input: @"|@page ""/""

<h1>Hello, world!</h1>

        Welcome to your new app.

<SurveyPrompt Title=""How is Blazor working for you?"" />

<div class=""FF""
     id=""ERT"">
     asdf
    <div class=""3""
         id=""3"">
             @if(true){<p></p>}
         </div>
</div>

@{
<div class=""FF""
    id=""ERT"">
    asdf
    <div class=""3""
        id=""3"">
            @if(true){<p></p>}
        </div>
</div>
}

@functions {
        public class Foo
    {
        @* This is a Razor Comment *@
        void Method() { }
    }
}
|",
expected: @"@page ""/""

<h1>Hello, world!</h1>

        Welcome to your new app.

<SurveyPrompt Title=""How is Blazor working for you?"" />

<div class=""FF""
     id=""ERT"">
    asdf
    <div class=""3""
         id=""3"">
        @if (true)
        {
            <p></p>
        }
    </div>
</div>

@{
    <div class=""FF""
         id=""ERT"">
        asdf
        <div class=""3""
             id=""3"">
            @if (true)
            {
                <p></p>
            }
        </div>
    </div>
}

@functions {
    public class Foo
    {
        @* This is a Razor Comment *@
        void Method() { }
    }
}
");
        }
    }
}
