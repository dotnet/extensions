// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class DefaultTagHelperDescriptionFactoryTest
    {
        [Fact]
        public void ResolveTagHelperTypeName_ExtractsTypeName_SimpleReturnType()
        {
            // Arrange
            var info = new TagHelperAttributeDescriptionInfo(
                displayName: "string SomeTypeName.SomePropertyName",
                propertyName: "SomePropertyName",
                returnTypeName: "System.String",
                documentation: string.Empty);

            // Act
            var typeName = DefaultTagHelperDescriptionFactory.ResolveTagHelperTypeName(info);

            // Assert
            Assert.Equal("SomeTypeName", typeName);
        }

        [Fact]
        public void ResolveTagHelperTypeName_ExtractsTypeName_ComplexReturnType()
        {
            // Arrange
            var info = new TagHelperAttributeDescriptionInfo(
                displayName: "SomeReturnTypeName SomeTypeName.SomePropertyName",
                propertyName: "SomePropertyName",
                returnTypeName: "SomeReturnTypeName",
                documentation: string.Empty);

            // Act
            var typeName = DefaultTagHelperDescriptionFactory.ResolveTagHelperTypeName(info);

            // Assert
            Assert.Equal("SomeTypeName", typeName);
        }

        [Fact]
        public void GetSimpleName_NoopsForNonPrimitiveType()
        {
            // Arrange
            var typeName = "Microsoft.AspNetCore.SomeType";

            // Act
            var result = DefaultTagHelperDescriptionFactory.GetSimpleName(typeName);

            // Assert
            Assert.Equal(typeName, result);
        }

        [Theory]
        [InlineData("System.Int32", "int")]
        [InlineData("System.Boolean", "bool")]
        [InlineData("System.String", "string")]
        public void GetSimpleName_SimplifiesPrimitiveTypes(string typeName, string expectedTypeName)
        {
            // Arrange

            // Act
            var result = DefaultTagHelperDescriptionFactory.GetSimpleName(typeName);

            // Assert
            Assert.Equal(expectedTypeName, result);
        }

        [Fact]
        public void ReduceTypeName_Plain()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceTypeName(content);

            // Assert
            Assert.Equal("SomeTypeName", reduced);
        }

        [Fact]
        public void ReduceTypeName_Generics()
        {
            // Arrange
            var content = "System.Collections.Generic.List<System.String>";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceTypeName(content);

            // Assert
            Assert.Equal("List<System.String>", reduced);
        }

        [Fact]
        public void ReduceTypeName_CrefGenerics()
        {
            // Arrange
            var content = "System.Collections.Generic.List{System.String}";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceTypeName(content);

            // Assert
            Assert.Equal("List{System.String}", reduced);
        }

        [Fact]
        public void ReduceTypeName_NestedGenerics()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar<Baz.Phi>>";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceTypeName(content);

            // Assert
            Assert.Equal("SomeType<Foo.Bar<Baz.Phi>>", reduced);
        }

        [Theory]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar<Baz.Phi>>")]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar{Baz.Phi}}")]
        public void ReduceTypeName_UnbalancedDocs_NotRecoverable_ReturnsOriginalContent(string content)
        {
            // Arrange

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceTypeName(content);

            // Assert
            Assert.Equal(content, reduced);
        }

        [Fact]
        public void ReduceMemberName_Plain()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeProperty";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceMemberName(content);

            // Assert
            Assert.Equal("SomeType.SomeProperty", reduced);
        }

        [Fact]
        public void ReduceMemberName_Generics()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar>.SomeProperty<Foo.Bar>";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceMemberName(content);

            // Assert
            Assert.Equal("SomeType<Foo.Bar>.SomeProperty<Foo.Bar>", reduced);
        }

        [Fact]
        public void ReduceMemberName_CrefGenerics()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType{Foo.Bar}.SomeProperty{Foo.Bar}";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceMemberName(content);

            // Assert
            Assert.Equal("SomeType{Foo.Bar}.SomeProperty{Foo.Bar}", reduced);
        }

        [Fact]
        public void ReduceMemberName_NestedGenericsMethodsTypes()
        {
            // Arrange
            var content = "Microsoft.AspNetCore.SometTagHelpers.SomeType<Foo.Bar<Baz,Fi>>.SomeMethod(Foo.Bar<System.String>,Baz<Something>.Fi)";

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceMemberName(content);

            // Assert
            Assert.Equal("SomeType<Foo.Bar<Baz,Fi>>.SomeMethod(Foo.Bar<System.String>,Baz<Something>.Fi)", reduced);
        }

        [Theory]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar<Baz.Phi>>")]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar{Baz.Phi}}")]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo.Bar(Baz.Phi))")]
        [InlineData("Microsoft.AspNetCore.SometTagHelpers.SomeType.Foo{.>")]
        public void ReduceMemberName_UnbalancedDocs_NotRecoverable_ReturnsOriginalContent(string content)
        {
            // Arrange

            // Act
            var reduced = DefaultTagHelperDescriptionFactory.ReduceMemberName(content);

            // Assert
            Assert.Equal(content, reduced);
        }

        [Fact]
        public void ReduceCrefValue_InvalidShortValue_ReturnsEmptyString()
        {
            // Arrange
            var content = "T:";

            // Act
            var value = DefaultTagHelperDescriptionFactory.ReduceCrefValue(content);

            // Assert
            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void ReduceCrefValue_InvalidUnknownIdentifierValue_ReturnsEmptyString()
        {
            // Arrange
            var content = "X:";

            // Act
            var value = DefaultTagHelperDescriptionFactory.ReduceCrefValue(content);

            // Assert
            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void ReduceCrefValue_Type()
        {
            // Arrange
            var content = "T:Microsoft.AspNetCore.SometTagHelpers.SomeType";

            // Act
            var value = DefaultTagHelperDescriptionFactory.ReduceCrefValue(content);

            // Assert
            Assert.Equal("SomeType", value);
        }

        [Fact]
        public void ReduceCrefValue_Property()
        {
            // Arrange
            var content = "P:Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeProperty";

            // Act
            var value = DefaultTagHelperDescriptionFactory.ReduceCrefValue(content);

            // Assert
            Assert.Equal("SomeType.SomeProperty", value);
        }

        [Fact]
        public void ReduceCrefValue_Member()
        {
            // Arrange
            var content = "P:Microsoft.AspNetCore.SometTagHelpers.SomeType.SomeMember";

            // Act
            var value = DefaultTagHelperDescriptionFactory.ReduceCrefValue(content);

            // Assert
            Assert.Equal("SomeType.SomeMember", value);
        }

        [Fact]
        public void TryExtractSummary_ExtractsSummary_ReturnsTrue()
        {
            // Arrange
            var expectedSummary = " Hello World ";
            var documentation = $@"
Prefixed invalid content


<summary>{expectedSummary}</summary>

Suffixed invalid content";

            // Act
            var result = DefaultTagHelperDescriptionFactory.TryExtractSummary(documentation, out var summary);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedSummary, summary);
        }

        [Fact]
        public void TryExtractSummary_NoStartSummary_ReturnsFalse()
        {
            // Arrange
            var documentation = @"
Prefixed invalid content


</summary>

Suffixed invalid content";

            // Act
            var result = DefaultTagHelperDescriptionFactory.TryExtractSummary(documentation, out var summary);

            // Assert
            Assert.False(result);
            Assert.Null(summary);
        }

        [Fact]
        public void TryExtractSummary_NoEndSummary_ReturnsFalse()
        {
            // Arrange
            var documentation = @"
Prefixed invalid content


<summary>

Suffixed invalid content";

            // Act
            var result = DefaultTagHelperDescriptionFactory.TryExtractSummary(documentation, out var summary);

            // Assert
            Assert.False(result);
            Assert.Null(summary);
        }

        [Fact]
        public void CleanSummaryContent_ReplacesSeeCrefs()
        {
            // Arrange
            var summary = "Accepts <see cref=\"T:System.Collections.List{System.String}\" />s";

            // Act
            var cleanedSummary = DefaultTagHelperDescriptionFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
        }

        [Fact]
        public void CleanSummaryContent_ReplacesSeeAlsoCrefs()
        {
            // Arrange
            var summary = "Accepts <seealso cref=\"T:System.Collections.List{System.String}\" />s";

            // Act
            var cleanedSummary = DefaultTagHelperDescriptionFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal("Accepts `List<System.String>`s", cleanedSummary);
        }

        [Fact]
        public void CleanSummaryContent_TrimsSurroundingWhitespace()
        {
            // Arrange
            var summary = @"
            Hello

    World
";

            // Act
            var cleanedSummary = DefaultTagHelperDescriptionFactory.CleanSummaryContent(summary);

            // Assert
            Assert.Equal(@"
Hello

World
", cleanedSummary);
        }

        [Fact]
        public void TryCreateDescription_NoAssociatedTagHelperDescriptions_ReturnsFalse()
        {
            // Arrange
            var descriptionFactory = new DefaultTagHelperDescriptionFactory();
            var elementDescription = ElementDescriptionInfo.Default;

            // Act
            var result = descriptionFactory.TryCreateDescription(elementDescription, out var markdown);

            // Assert
            Assert.False(result);
            Assert.Null(markdown);
        }

        [Fact]
        public void TryCreateDescription_Element_SingleAssociatedTagHelper_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultTagHelperDescriptionFactory();
            var associatedTagHelperInfos = new[]
            {
                new TagHelperDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var elementDescription = new ElementDescriptionInfo(associatedTagHelperInfos);

            // Act
            var result = descriptionFactory.TryCreateDescription(elementDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**SomeTagHelper**

Uses `List<System.String>`s
", markdown);
        }

        [Fact]
        public void TryCreateDescription_Element_MultipleAssociatedTagHelpers_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultTagHelperDescriptionFactory();
            var associatedTagHelperInfos = new[]
            {
                new TagHelperDescriptionInfo("Microsoft.AspNetCore.SomeTagHelper", "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
                new TagHelperDescriptionInfo("Microsoft.AspNetCore.OtherTagHelper", "<summary>Also uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var elementDescription = new ElementDescriptionInfo(associatedTagHelperInfos);

            // Act
            var result = descriptionFactory.TryCreateDescription(elementDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**SomeTagHelper**

Uses `List<System.String>`s

---
**OtherTagHelper**

Also uses `List<System.String>`s
", markdown);
        }

        [Fact]
        public void TryCreateDescription_Attribute_SingleAssociatedAttribute_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultTagHelperDescriptionFactory();
            var associatedAttributeDescriptions = new[]
            {
                new TagHelperAttributeDescriptionInfo(
                    displayName: "string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty",
                    propertyName: "SomeProperty",
                    returnTypeName: "System.String",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>")
            };
            var attributeDescription = new AttributeDescriptionInfo(associatedAttributeDescriptions);

            // Act
            var result = descriptionFactory.TryCreateDescription(attributeDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s
", markdown);
        }

        [Fact]
        public void TryCreateDescription_Attribute_MultipleAssociatedAttributes_ReturnsTrue()
        {
            // Arrange
            var descriptionFactory = new DefaultTagHelperDescriptionFactory();
            var associatedAttributeDescriptions = new[]
            {
                new TagHelperAttributeDescriptionInfo(
                    displayName: "string Microsoft.AspNetCore.SomeTagHelpers.SomeTypeName.SomeProperty",
                    propertyName: "SomeProperty",
                    returnTypeName: "System.String",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
                new TagHelperAttributeDescriptionInfo(
                    displayName: "System.Boolean? Microsoft.AspNetCore.SomeTagHelpers.AnotherTypeName.AnotherProperty",
                    propertyName: "AnotherProperty",
                    returnTypeName: "System.Boolean?",
                    documentation: "<summary>Uses <see cref=\"T:System.Collections.List{System.String}\" />s</summary>"),
            };
            var attributeDescription = new AttributeDescriptionInfo(associatedAttributeDescriptions);

            // Act
            var result = descriptionFactory.TryCreateDescription(attributeDescription, out var markdown);

            // Assert
            Assert.True(result);
            Assert.Equal(@"**string** SomeTypeName.**SomeProperty**

Uses `List<System.String>`s

---
**Boolean?** AnotherTypeName.**AnotherProperty**

Uses `List<System.String>`s
", markdown);
        }
    }
}
