// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    public class HtmlFormattingTest : FormattingTestBase
    {
        internal override bool UseTwoPhaseCompilation => true;

        internal override bool DesignTime => true;

        [Fact]
        public async Task FormatsSimpleHtmlTag()
        {
            await RunFormattingTestAsync(
input: @"
   <html>
<head>
   <title>Hello</title></head>
<body><div>
</div>
        </body>
 </html>
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
input: @"@page ""/error""

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
",
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
input: @"@page ""/test""
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
",
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
input: @"@page ""/test""

<div class=@className>Some Text</div>

@{
@: Hi!
var x = 123;
<p>
        @if (true) {
                var t = 1;
if (true)
{
<div>@DateTime.Now</div>
            }

            @while(true){
 }
        }
        </p>
}
",
expected: @"@page ""/test""

<div class=@className>Some Text</div>

@{
    @: Hi!
    var x = 123;
    <p>
        @if (true)
        {
            var t = 1;
            if (true)
            {
                <div>@DateTime.Now</div>
            }

            @while (true)
            {
            }
        }
    </p>
}
");
        }

        [Fact]
        public async Task FormatsMixedContentWithMultilineExpressions()
        {
            await RunFormattingTestAsync(
input: @"@page ""/test""

<div
attr='val'
class=@className>Some Text</div>

@{
@: Hi!
var x = DateTime
    .Now.ToString();
<p>
        @if (true) {
                var t = 1;
        }
        </p>
}

@(DateTime
    .Now
.ToString())

@(
    Foo.Values.Select(f =>
    {
        return f.ToString();
    })
)
",
expected: @"@page ""/test""

<div attr='val'
     class=@className>
    Some Text
</div>

@{
    @: Hi!
    var x = DateTime
        .Now.ToString();
    <p>
        @if (true)
        {
            var t = 1;
        }
    </p>
}

@(DateTime
    .Now
.ToString())

@(
    Foo.Values.Select(f =>
    {
        return f.ToString();
    })
)
");
        }

        [Fact]
        public async Task FormatsComplexBlock()
        {
            await RunFormattingTestAsync(
input: @"@page ""/""

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
",
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

        [Fact]
        public async Task FormatsComponentTags()
        {
            var tagHelpers = GetComponents();
            await RunFormattingTestAsync(
input: @"
   <Counter>
    @if(true){
        <p>@DateTime.Now</p>
}
</Counter>

    <GridTable>
    @foreach (var row in rows){
        <GridRow @onclick=""SelectRow(row)"">
        @foreach (var cell in row){
    <GridCell>@cell</GridCell>}</GridRow>
    }
</GridTable>
",
expected: @"
<Counter>
    @if (true)
    {
        <p>@DateTime.Now</p>
    }
</Counter>

<GridTable>
    @foreach (var row in rows)
    {
        <GridRow @onclick=""SelectRow(row)"">
            @foreach (var cell in row)
            {
                <GridCell>@cell</GridCell>
            }
        </GridRow>
    }
</GridTable>
",
tagHelpers: tagHelpers);
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/aspnetcore/issues/26836")]
        public async Task FormatNestedBlock()
        {
            await RunFormattingTestAsync(
input: @"@code {
    public string DoSomething()
    {
        <strong>
            @DateTime.Now.ToString()
        </strong>

        return String.Empty;
    }
}
",
expected: @"@code {
    public string DoSomething()
    {
        <strong>
            @DateTime.Now.ToString()
        </strong>

        return String.Empty;
    }
}
");
        }

        private IReadOnlyList<TagHelperDescriptor> GetComponents()
        {
            AdditionalSyntaxTrees.Add(Parse(@"
using Microsoft.AspNetCore.Components;
namespace Test
{
    public class Counter : ComponentBase
    {
        [Parameter]
        public int IncrementAmount { get; set; }
    }

    public class GridTable : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }

    public class GridRow : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }

    public class GridCell : ComponentBase
    {
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}
"));

            var generated = CompileToCSharp("Test.razor", string.Empty, throwOnFailure: false, fileKind: FileKinds.Component);
            var tagHelpers = generated.CodeDocument.GetTagHelperContext().TagHelpers;
            return tagHelpers;
        }
    }
}
