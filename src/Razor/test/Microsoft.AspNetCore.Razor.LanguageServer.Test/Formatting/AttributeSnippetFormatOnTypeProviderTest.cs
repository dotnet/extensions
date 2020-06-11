// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class AttributeSnippetFormatOnTypeProviderTest : FormatOnTypeProviderTestBase
    {
        [Fact]
        public void OnTypeEqual_AfterTagHelperIntAttribute_TriggersAttributeValueSnippet()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute|></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute=""{ LanguageServerConstants.CursorPlaceholderString}""></span>
</section>
",
character: "=",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterNonTagHelperAttribute_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span test2|></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span test2=></span>
</section>
",
character: "=",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperStringAttribute_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span stringAttribute|></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span stringAttribute=></span>
</section>
",
character: "=",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperTag_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span|></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span=></span>
</section>
",
character: "=",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperAttributeEqual_Noops()
        {
            RunFormatOnTypeTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute=|></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute==></span>
</section>
",
character: "=",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        internal override RazorFormatOnTypeProvider CreateProvider()
        {
            var provider = new AttributeSnippetFormatOnTypeProvider(new DefaultTagHelperFactsService());
            return provider;
        }

        TagHelperDescriptor[] TagHelpers
        {
            get
            {

                var descriptor = TagHelperDescriptorBuilder.Create("SpanTagHelper", "TestAssembly");
                descriptor.SetTypeName("TestNamespace.SpanTagHelper");
                descriptor.TagMatchingRule(builder => builder.RequireTagName("span"));
                descriptor.BindAttribute(builder =>
                    builder
                        .Name("intAttribute")
                        .PropertyName("intAttribute")
                        .TypeName(typeof(int).FullName));
                descriptor.BindAttribute(builder =>
                    builder
                        .Name("stringAttribute")
                        .PropertyName("stringAttribute")
                        .TypeName(typeof(string).FullName));

                return new[]
                {
                    descriptor.Build()
                };
            }
        }
    }
}
