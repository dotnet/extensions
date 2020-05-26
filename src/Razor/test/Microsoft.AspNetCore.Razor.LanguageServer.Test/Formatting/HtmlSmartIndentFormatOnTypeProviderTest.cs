// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Formatting
{
    public class HtmlSmartIndentFormatOnTypeProviderTest : FormatOnTypeProviderTestBase
    {
        public static IReadOnlyList<TagHelperDescriptor> TagHelpers
        {
            get
            {
                var descriptor = TagHelperDescriptorBuilder.Create("SpanTagHelper", "TestAssembly");
                descriptor.SetTypeName("TestNamespace.SpanTagHelper");
                descriptor.TagMatchingRule(builder => builder.RequireTagName("span"));
                descriptor.BindAttribute(builder =>
                    builder
                        .Name("test")
                        .PropertyName("test")
                        .TypeName(typeof(string).FullName));
                return new[]
                {
                    descriptor.Build()
                };
            }
        }

        [Fact]
        public void FormatOnType_VoidHtmlTag_Noops()
        {
            RunFormatOnTypeTest(
input: @"<input>|<strong></strong>
",
expected: @"<input>
<strong></strong>
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_SelfClosingHtmlTag_Noops()
        {
            RunFormatOnTypeTest(
input: @"<input />|<strong></strong>
",
expected: @"<input />
<strong></strong>
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_SelfClosingVoidHtmlTag_Noops()
        {
            RunFormatOnTypeTest(
input: @"<input />|<input>
",
expected: @"<input />
<input>
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_HtmlTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"<strong>|</strong>
",
expected: $@"<strong>
    {LanguageServerConstants.CursorPlaceholderString}
</strong>
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_NestedHtmlTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"<section>
    <div>|</div>
</section>
",
expected: $@"<section>
    <div>
        {LanguageServerConstants.CursorPlaceholderString}
    </div>
</section>
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_ComplexHtmlTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"
@if (true)
{
    <section class='column-1'>@{<hr>
        <div onclick='invokeMethod(""Some Content</div>"")'>|</div><input />
    }
    <hr /></section>
}
",
expected: $@"
@if (true)
{{
    <section class='column-1'>@{{<hr>
        <div onclick='invokeMethod(""Some Content</div>"")'>
            {LanguageServerConstants.CursorPlaceholderString}
        </div><input />
    }}
    <hr /></section>
}}
",
character: Environment.NewLine);
        }

        [Fact]
        public void FormatOnType_TagHelperTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<span>|</span>
",
expected: $@"
@addTagHelper *, TestAssembly

<span>
    {LanguageServerConstants.CursorPlaceholderString}
</span>
",
character: Environment.NewLine,
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void FormatOnType_NestedTagHelperTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span>|</span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span>
        {LanguageServerConstants.CursorPlaceholderString}
    </span>
</section>
",
character: Environment.NewLine,
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void FormatOnType_ComplexTagHelperTag_SmartIndents()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly


@if (true)
{
    <section class='column-1'>@{<hr>
        <span onclick='invokeMethod(""Some Content</span>"")' test='hello world'>|</span><input />
    }
    <hr /></section>
}
",
expected: $@"
@addTagHelper *, TestAssembly


@if (true)
{{
    <section class='column-1'>@{{<hr>
        <span onclick='invokeMethod(""Some Content</span>"")' test='hello world'>
            {LanguageServerConstants.CursorPlaceholderString}
        </span><input />
    }}
    <hr /></section>
}}
",
character: Environment.NewLine,
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        internal override RazorFormatOnTypeProvider CreateProvider() => new HtmlSmartIndentFormatOnTypeProvider();
    }
}
