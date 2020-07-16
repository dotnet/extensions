// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class DefaultRazorFormattingServiceTest : LanguageServerTestBase
    {
        [Fact]
        public async Task FormatsCodeBlockDirective()
        {
            await RunFormattingTestAsync(
input: @"
|@code {
 public class Foo{}
        public interface Bar {
}
}|
",
expected: @"
@code {
    public class Foo { }
    public interface Bar
    {
    }
}
");
        }

        [Fact]
        public async Task DoesNotFormat_NonCodeBlockDirectives()
        {
            await RunFormattingTestAsync(
input: @"
|@{
var x = ""foo"";
}
<div>
        </div>|
",
expected: @"
@{
var x = ""foo"";
}
<div>
        </div>
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithMarkup()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{
void Method() { <div></div> }
}
}|
",
expected: @"
@functions {
 public class Foo{
void Method() { <div></div> }
}
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithImplicitExpressions()
        {
            await RunFormattingTestAsync(
input: @"
|@code {
 public class Foo{
void Method() { @DateTime.Now }
}
}|
",
expected: @"
@code {
 public class Foo{
void Method() { @DateTime.Now }
}
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithExplicitExpressions()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{
void Method() { @(DateTime.Now) }
}
}|
",
expected: @"
@functions {
 public class Foo{
void Method() { @(DateTime.Now) }
}
}
",
fileKind: FileKinds.Legacy);
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithRazorComments()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{
@* This is a Razor Comment *@
void Method() {  }
}
}|
",
expected: @"
@functions {
 public class Foo{
@* This is a Razor Comment *@
void Method() {  }
}
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirectiveWithRazorStatements()
        {
            await RunFormattingTestAsync(
input: @"
|@functions {
 public class Foo{
@* This is a Razor Comment *@
void Method() { @if (true) {} }
}
}|
",
expected: @"
@functions {
 public class Foo{
@* This is a Razor Comment *@
void Method() { @if (true) {} }
}
}
");
        }

        [Fact]
        public async Task DoesNotFormat_CodeBlockDirective_NotInSelectedRange()
        {
            await RunFormattingTestAsync(
input: @"
|<div>Foo</div>|
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
        |public interface Bar {
}|
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
|@functions {
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
}|
",
expected: @"
@functions {
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
input: @"|
Hello World
@code {
public class HelloWorld
{
}
}

@functions{
    
 public class Bar {}
}
|",
expected: @"
Hello World
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
|@functions {public class Foo{
}
}|
",
expected: @"
@functions {
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
|@functions {
public class Foo{
}}|
",
expected: @"
@functions {
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
|@functions {public class Foo{}}|
",
expected: @"
@functions {
    public class Foo { }
}
");
        }

        [Fact]
        public async Task IndentsCodeBlockDirectiveStart()
        {
            await RunFormattingTestAsync(
input: @"|
Hello World
     @functions {public class Foo{}
}|
",
expected: @"
Hello World
@functions {
    public class Foo { }
}
");
        }

        [Fact]
        public async Task IndentsCodeBlockDirectiveEnd()
        {
            await RunFormattingTestAsync(
input: @"|
 @functions {
public class Foo{}
     }|
",
expected: @"
@functions {
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
|@functions{
     public class Foo
            {
                public Foo()
                {
                    var arr = new string[ ] {
""One"", ""two"",
""three""
                    };
                }
public int MyProperty { get
{
return 0 ;
} set {} }

void Method(){

}
                    }
}|
",
expected: @"
@using System.Buffers
@functions{
    public class Foo
    {
        public Foo()
        {
            var arr = new string[] {
""One"", ""two"",
""three""
                };
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
|@code {
 public class Foo{}
        void Method(  ) {
}
}|
",
expected: @"
@code {
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
|@code {
 public class Foo{}
        void Method(  ) {
}
}|
",
expected: @"
@code {
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
|@code {
 public class Foo{}
        void Method(  ) {
}
}|
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
|@code {
 public class Foo{}
        void Method(  ) {
}
}|
",
expected: @"
@code {
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
|@code {
 public class Foo{}
        void Method(  ) {
}
}|
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

        private async Task RunFormattingTestAsync(string input, string expected, int tabSize = 4, bool insertSpaces = true, string fileKind = default)
        {
            // Arrange
            var start = input.IndexOf('|');
            var end = input.LastIndexOf('|');
            input = input.Replace("|", string.Empty);

            var source = SourceText.From(input);
            var span = TextSpan.FromBounds(start, end - 1);
            var range = span.AsRange(source);

            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var codeDocument = CreateCodeDocument(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var formattingService = CreateFormattingService(codeDocument);

            // Act
            var edits = await formattingService.FormatAsync(uri, codeDocument, range, options);

            // Assert
            var edited = ApplyEdits(source, edits);
            var actual = edited.ToString();
            Assert.Equal(expected, actual);
        }

        private RazorFormattingService CreateFormattingService(RazorCodeDocument codeDocument)
        {
            var mappingService = new DefaultRazorDocumentMappingService();
            var filePathNormalizer = new FilePathNormalizer();

            var client = new FormattingLanguageServerClient();
            client.AddCodeDocument(codeDocument);
            var languageServer = Mock.Of<ILanguageServer>(ls => ls.Client == client);


            return new DefaultRazorFormattingService(mappingService, filePathNormalizer, languageServer, LoggerFactory);
        }

        private SourceText ApplyEdits(SourceText source, TextEdit[] edits)
        {
            var changes = edits.Select(e => e.AsTextChange(source));
            return source.WithChanges(changes);
        }

        private static RazorCodeDocument CreateCodeDocument(SourceText text, string path, IReadOnlyList<TagHelperDescriptor> tagHelpers = null, string fileKind = default)
        {
            fileKind ??= FileKinds.Component;
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = text.GetRazorSourceDocument(path, path);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }
    }
}
