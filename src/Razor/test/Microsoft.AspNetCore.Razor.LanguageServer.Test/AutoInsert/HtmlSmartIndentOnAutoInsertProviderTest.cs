// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public class HtmlSmartIndentOnAutoInsertProviderTest : RazorOnAutoInsertProviderTestBase
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
        public void TryResolveInsertion_VoidHtmlTag_Noops()
        {
            RunAutoInsertTest(
input: @"<input>
$$<strong></strong>
",
expected: @"<input>
<strong></strong>
");
        }

        [Fact]
        public void TryResolveInsertion_SelfClosingHtmlTag_Noops()
        {
            RunAutoInsertTest(
input: @"<input />
$$<strong></strong>
",
expected: @"<input />
<strong></strong>
");
        }

        [Fact]
        public void TryResolveInsertion_SelfClosingVoidHtmlTag_Noops()
        {
            RunAutoInsertTest(
input: @"<input />
$$<input>
",
expected: @"<input />
<input>
");
        }

        [Fact]
        public void TryResolveInsertion_HtmlTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"<strong>
$$</strong>
",
expected: @"<strong>
    $0
</strong>
");
        }

        [Fact]
        public void TryResolveInsertion_NestedHtmlTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"<section>
    <div>
$$</div>
</section>
",
expected: @"<section>
    <div>
        $0
    </div>
</section>
");
        }

        [Fact]
        public void TryResolveInsertion_ComplexHtmlTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"
@if (true)
{
    <section class='column-1'>@{<hr>
        <div onclick='invokeMethod(""Some Content</div>"")'>
$$</div><input />
    }
    <hr /></section>
}
",
expected: @"
@if (true)
{
    <section class='column-1'>@{<hr>
        <div onclick='invokeMethod(""Some Content</div>"")'>
            $0
        </div><input />
    }
    <hr /></section>
}
");
        }

        [Fact]
        public void TryResolveInsertion_TagHelperTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<span>
$$</span>
",
expected: @"
@addTagHelper *, TestAssembly

<span>
    $0
</span>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void TryResolveInsertion_NestedTagHelperTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span>
$$</span>
</section>
",
expected: @"
@addTagHelper *, TestAssembly

<section>
    <span>
        $0
    </span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void TryResolveInsertion_ComplexTagHelperTag_SmartIndents()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly


@if (true)
{
    <section class='column-1'>@{<hr>
        <span onclick='invokeMethod(""Some Content</span>"")' test='hello world'>
$$</span><input />
    }
    <hr /></section>
}
",
expected: @"
@addTagHelper *, TestAssembly


@if (true)
{
    <section class='column-1'>@{<hr>
        <span onclick='invokeMethod(""Some Content</span>"")' test='hello world'>
            $0
        </span><input />
    }
    <hr /></section>
}
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        internal override RazorOnAutoInsertProvider CreateProvider() => new HtmlSmartIndentOnAutoInsertProvider();
    }
}
