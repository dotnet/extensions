// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.VisualStudio.Editor.Razor;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public class AttributeSnippetOnAutoInsertProviderTest : RazorOnAutoInsertProviderTestBase
    {
        [Fact]
        public void OnTypeEqual_AfterTagHelperIntAttribute_TriggersAttributeValueSnippet()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute=$$></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute=""$0""></span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterNonTagHelperAttribute_Noops()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span test2=$$></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span test2=></span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperStringAttribute_Noops()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span stringAttribute=$$></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span stringAttribute=></span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperTag_Noops()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span=$$></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span=></span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        [Fact]
        public void OnTypeEqual_AfterTagHelperAttributeEqual_Noops()
        {
            RunAutoInsertTest(
input: @"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute==$$></span>
</section>
",
expected: $@"
@addTagHelper *, TestAssembly

<section>
    <span intAttribute==></span>
</section>
",
fileKind: FileKinds.Legacy,
tagHelpers: TagHelpers);
        }

        internal override RazorOnAutoInsertProvider CreateProvider()
        {
            var provider = new AttributeSnippetOnAutoInsertProvider(new DefaultTagHelperFactsService());
            return provider;
        }

        static TagHelperDescriptor[] TagHelpers
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
