// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class CodeDirectiveFormattingTest : FormattingTestBase
    {
        [Fact]
        public async Task FormatsCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        public interface Bar {
}
}
",
expected: @"@code {
    public class Foo { }
    public interface Bar
    {
    }
}
");
        }

        [Fact]
        public async Task Formats_NonCodeBlockDirectives()
        {
            await RunFormattingTestAsync(
input: @"
@{
var x = ""foo"";
}
<div>
        </div>
",
expected: @"@{
    var x = ""foo"";
}
<div>
</div>
");
        }

        [Fact]
        public async Task Formats_CodeBlockDirectiveWithMarkup()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{
void Method() { <div></div> }
}
}
",
expected: @"@functions {
    public class Foo
    {
        void Method()
        { 
            <div></div>
        }
    }
}
");
        }

        [Fact]
        public async Task Formats_CodeBlockDirectiveWithImplicitExpressions()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{
void Method() { @DateTime.Now }
    }
}
",
expected: @"@code {
    public class Foo
    {
        void Method()
        { 
            @DateTime.Now
        }
    }
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithExplicitExpressions()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{
void Method() { @(DateTime.Now) }
    }
}
",
expected: @"@functions {
    public class Foo
    {
        void Method()
        { 
            @(DateTime.Now)
        }
    }
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task DoesNotFormat_SectionDirectiveBlock()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{
void Method() {  }
    }
}

@section Scripts {
<script></script>
}
",
expected: @"@functions {
    public class Foo
    {
        void Method() { }
    }
}

@section Scripts {
<script></script>
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task Formats_CodeBlockDirectiveWithRazorComments()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{
@* This is a Razor Comment *@
void Method() {  }
}
}
",
expected: @"@functions {
    public class Foo
    {
        @* This is a Razor Comment *@
        void Method() { }
    }
}
");
        }

        [Fact]
        public async Task Formats_CodeBlockDirectiveWithRazorStatements()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{
@* This is a Razor Comment *@
    }
}
",
expected: @"@functions {
    public class Foo
    {
        @* This is a Razor Comment *@
    }
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirective_NotInSelectedRange()
        {
            await RunFormattingTestAsync(
input: @"
[|<div>Foo</div>|]
@functions {
 public class Foo{}
        public interface Bar {
}
}
",
expected: @"
<div>Foo</div>
@functions {
 public class Foo{}
        public interface Bar {
}
}
");
        }

        [Fact]
        public async Task OnlyFormatsWithinRange()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{}
        [|public interface Bar {
}|]
}
",
expected: @"
@functions {
 public class Foo{}
    public interface Bar
    {
    }
}
");
        }

        [Fact]
        public async Task MultipleCodeBlockDirectives()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
 public class Foo{}
        public interface Bar {
}
}
Hello World
@functions {
      public class Baz    {
          void Method ( )
          { }
          }
}
",
expected: @"@functions {
    public class Foo { }
    public interface Bar
    {
    }
}
Hello World
@functions {
    public class Baz
    {
        void Method()
        { }
    }
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task MultipleCodeBlockDirectives2()
        {
            await RunFormattingTestAsync(
input: @"
Hello World
@code {
public class HelloWorld
{
}
}

@functions{

 public class Bar {}
}
",
expected: @"Hello World
@code {
    public class HelloWorld
    {
    }
}

@functions{

    public class Bar { }
}
");
        }

        [Fact]
        public async Task CodeOnTheSameLineAsCodeBlockDirectiveStart()
        {
            await RunFormattingTestAsync(
input: @"
@functions {public class Foo{
}
}
",
expected: @"@functions {
    public class Foo
    {
    }
}
");
        }

        [Fact]
        public async Task CodeOnTheSameLineAsCodeBlockDirectiveEnd()
        {
            await RunFormattingTestAsync(
input: @"
@functions {
public class Foo{
}}
",
expected: @"@functions {
    public class Foo
    {
    }
}
");
        }

        [Fact]
        public async Task SingleLineCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
@functions {public class Foo{}
}
",
expected: @"@functions {
    public class Foo { }
}
");
        }

        [Fact]
        public async Task IndentsCodeBlockDirectiveStart()
        {
            await RunFormattingTestAsync(
input: @"
Hello World
     @functions {public class Foo{}
}
",
expected: @"Hello World
     @functions {
    public class Foo { }
}
");
        }

        [Fact]
        public async Task IndentsCodeBlockDirectiveEnd()
        {
            await RunFormattingTestAsync(
input: @"
 @functions {
public class Foo{}
     }
",
expected: @" @functions {
    public class Foo { }
     }
");
        }

        [Fact]
        public async Task ComplexCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
@using System.Buffers
@functions{
     public class Foo
            {
                public Foo()
                {
                    var arr = new string[ ] {
""One"", ""two"",
""three""
                    };
                    var str = @""
This should
not
be indented.
"";
                }
public int MyProperty { get
{
return 0 ;
} set {} }

void Method(){

}
                    }
}
",
expected: @"@using System.Buffers
@functions{
    public class Foo
    {
        public Foo()
        {
            var arr = new string[] {
""One"", ""two"",
""three""
                };
            var str = @""
This should
not
be indented.
"";
        }
        public int MyProperty
        {
            get
            {
                return 0;
            }
            set { }
        }

        void Method()
        {

        }
    }
}
");
        }

        [Fact]
        public async Task CodeBlockDirective_UseTabs()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        void Method(  ) {
}
}
",
expected: @"@code {
	public class Foo { }
	void Method()
	{
	}
}
",
insertSpaces: false);
        }

        [Fact]
        public async Task CodeBlockDirective_UseTabsWithTabSize8()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        void Method(  ) {
}
}
",
expected: @"@code {
	public class Foo { }
	void Method()
	{
	}
}
",
tabSize: 8,
insertSpaces: false);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/18996")]
        public async Task CodeBlockDirective_WithTabSize3()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        void Method(  ) {
}
}
",
expected: @"
@code {
   public class Foo { }
   void Method()
   {
   }
}
",
tabSize: 3);
        }

        [Fact]
        public async Task CodeBlockDirective_WithTabSize8()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        void Method(  ) {
}
}
",
expected: @"@code {
        public class Foo { }
        void Method()
        {
        }
}
",
tabSize: 8);
        }

        [Fact(Skip = "https://github.com/dotnet/aspnetcore/issues/18996")]
        public async Task CodeBlockDirective_WithTabSize12()
        {
            await RunFormattingTestAsync(
input: @"
@code {
 public class Foo{}
        void Method(  ) {
}
}
",
expected: @"
@code {
            public class Foo { }
            void Method()
            {
            }
}
",
tabSize: 12);
        }
    }
}
