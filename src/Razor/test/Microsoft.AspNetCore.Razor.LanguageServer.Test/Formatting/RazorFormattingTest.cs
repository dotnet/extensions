// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class RazorFormattingTest : FormattingTestBase
    {
        [Fact]
        public async Task CodeBlock_SpansMultipleLines()
        {
            await RunFormattingTestAsync(
input: @"
@code
        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code
{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_IndentedBlock_MaintainsIndent()
        {
            await RunFormattingTestAsync(
input: @"
<boo>
    @code
            {
        private int currentCount = 0;

        private void IncrementCount()
        {
            currentCount++;
        }
    }
</boo>
",
expected: @"
<boo>
    @code
    {
        private int currentCount = 0;

        private void IncrementCount()
        {
            currentCount++;
        }
    }
</boo>
");
        }

        [Fact]
        public async Task CodeBlock_TooMuchWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_NonSpaceWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code	{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task CodeBlock_NoWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@code{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
");
        }

        [Fact]
        public async Task FunctionsBlock_BraceOnNewLine()
        {
            await RunFormattingTestAsync(
input: @"
@functions
        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@functions
{
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task FunctionsBlock_TooManySpaces()
        {
            await RunFormattingTestAsync(
input: @"
@functions        {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
expected: @"@functions {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task Layout()
        {
            await RunFormattingTestAsync(
input: @"
@layout    MyLayout
",
expected: @"@layout MyLayout
");
        }

        [Fact]
        public async Task Inherits()
        {
            await RunFormattingTestAsync(
input: @"
@inherits    MyBaseClass
",
expected: @"@inherits MyBaseClass
");
        }

        [Fact]
        public async Task Inject()
        {
            await RunFormattingTestAsync(
input: @"
@inject    MyClass     myClass
",
expected: @"@inject MyClass myClass
");
        }

        [Fact]
        public async Task Inject_TrailingWhitespace()
        {
            await RunFormattingTestAsync(
input: @"
@inject    MyClass     myClass   
",
expected: @"@inject MyClass myClass
");
        }

        [Fact]
        public async Task Attribute()
        {
            await RunFormattingTestAsync(
input: @"
@attribute     [Obsolete(   ""asdf""   , error:    false)]
",
expected: @"@attribute [Obsolete(""asdf"", error: false)]
");
        }

        [Fact]
        public async Task Model()
        {
            await RunFormattingTestAsync(
input: @"
@model    MyModel
",
expected: @"@model MyModel
",
            fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task Page()
        {
            await RunFormattingTestAsync(
input: @"
@page    ""MyPage""
",
expected: @"@page ""MyPage""
",
            fileKind: FileKinds.Legacy);
        }

        // Regression prevention tests:
        [Fact]
        public async Task Using()
        {
            await RunFormattingTestAsync(
input: @"
@using   System;
",
expected: @"@using   System;
");
        }

        [Fact]
        public async Task UsingStatic()
        {
            await RunFormattingTestAsync(
input: @"
@using  static   System.Math;
",
expected: @"@using  static   System.Math;
");
        }

        [Fact]
        public async Task TagHelpers()
        {
            await RunFormattingTestAsync(
input: @"
@addTagHelper    *,    Microsoft.AspNetCore.Mvc.TagHelpers
@removeTagHelper    *,     Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper    ""*,  Microsoft.AspNetCore.Mvc.TagHelpers""
@removeTagHelper    ""*,  Microsoft.AspNetCore.Mvc.TagHelpers""
@tagHelperPrefix    th:
",
expected: @"@addTagHelper    *,    Microsoft.AspNetCore.Mvc.TagHelpers
@removeTagHelper    *,     Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper    ""*,  Microsoft.AspNetCore.Mvc.TagHelpers""
@removeTagHelper    ""*,  Microsoft.AspNetCore.Mvc.TagHelpers""
@tagHelperPrefix    th:
",
            fileKind: FileKinds.Legacy);
        }
    }
}
