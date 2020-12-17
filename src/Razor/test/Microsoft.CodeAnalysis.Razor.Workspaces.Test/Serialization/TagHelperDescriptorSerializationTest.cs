// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class TagHelperDescriptorSerializationTest
    {
        [Fact]
        public void TagHelperDescriptor_DefaultBlazorServerProject_RoundTrips()
        {
            // Arrange
            var testFileName = "test.taghelpers.json";
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, testFileName)))
            {
                current = current.Parent;
            }
            var tagHelperFilePath = Path.Combine(current.FullName, testFileName);
            var buffer = File.ReadAllBytes(tagHelperFilePath);
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new TagHelperDescriptorJsonConverter());
            IReadOnlyList<TagHelperDescriptor> deserializedTagHelpers;

            // Act
            using (var stream = new MemoryStream(buffer))
            using (var reader = new JsonTextReader(new StreamReader(stream)))
            {
                deserializedTagHelpers = serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }

            MemoryStream serializedStream;
            using (serializedStream = new MemoryStream())
            using (var writer = new StreamWriter(serializedStream, Encoding.UTF8, bufferSize: 4096))
            {
                serializer.Serialize(writer, deserializedTagHelpers);
            }

            IReadOnlyList<TagHelperDescriptor> reDeserializedTagHelpers;
            var reserializedStream = new MemoryStream(serializedStream.GetBuffer());
            using (reserializedStream)
            using (var reader = new JsonTextReader(new StreamReader(reserializedStream)))
            {
                reDeserializedTagHelpers = serializer.Deserialize<IReadOnlyList<TagHelperDescriptor>>(reader);
            }

            // Assert
            Assert.Equal(deserializedTagHelpers, reDeserializedTagHelpers);
        }
        
        [Fact]
        public void TagHelperDescriptor_CanReadCamelCasedData()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name")
                        .RequireTagStructure(TagStructure.WithoutEndTag),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one");
                    builder.AddMetadata("foo", "bar");
                    builder.AddDiagnostic(RazorDiagnostic.Create(
                        RazorDiagnosticFactory.TagHelper_InvalidTargetedTagNameNullOrWhitespace,
                        new SourceSpan("Test.razor", 5, 17, 18, 22)));
                });
            var serializerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    TagHelperDescriptorJsonConverter.Instance,
                    RazorDiagnosticJsonConverter.Instance,
                },
            };
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, serializerSettings);

            // Act
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name")
                        .RequireTagStructure(TagStructure.WithoutEndTag),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one");
                    builder.AddMetadata("foo", "bar");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void ViewComponentTagHelperDescriptor_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: "MVC.ViewComponent",
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name")
                        .RequireTagStructure(TagStructure.WithoutEndTag),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one");
                    builder.AddMetadata("foo", "bar");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_WithDiagnostic_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-two")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                            .Value("something")
                            .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                        .RequireParentTag("parent-name"),
                },
                configureAction: builder =>
                {
                    builder.AllowChildTag("allowed-child-one")
                        .AddMetadata("foo", "bar")
                        .AddDiagnostic(RazorDiagnostic.Create(
                            new RazorDiagnosticDescriptor("id", () => "Test Message", RazorDiagnosticSeverity.Error), new SourceSpan(null, 10, 20, 30, 40)));
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void TagHelperDescriptor_WithIndexerAttributes_RoundTripsProperly()
        {
            // Arrange
            var expectedDescriptor = CreateTagHelperDescriptor(
                kind: TagHelperConventions.DefaultKind,
                tagName: "tag-name",
                typeName: "type name",
                assemblyName: "assembly name",
                attributes: new Action<BoundAttributeDescriptorBuilder>[]
                {
                    builder => builder
                        .Name("test-attribute")
                        .PropertyName("TestAttribute")
                        .TypeName("SomeEnum")
                        .AsEnum()
                        .Documentation("Summary"),
                    builder => builder
                        .Name("test-attribute2")
                        .PropertyName("TestAttribute2")
                        .TypeName("SomeDictionary")
                        .AsDictionaryAttribute("dict-prefix-", "string"),
                },
                ruleBuilders: new Action<TagMatchingRuleDescriptorBuilder>[]
                {
                    builder => builder
                        .RequireAttributeDescriptor(attribute => attribute
                            .Name("required-attribute-one")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch))
                },
                configureAction: builder =>
                {
                    builder
                        .AllowChildTag("allowed-child-one")
                        .AddMetadata("foo", "bar")
                        .TagOutputHint("Hint");
                });

            // Act
            var serializedDescriptor = JsonConvert.SerializeObject(expectedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);
            var descriptor = JsonConvert.DeserializeObject<TagHelperDescriptor>(serializedDescriptor, TagHelperDescriptorJsonConverter.Instance, RazorDiagnosticJsonConverter.Instance);

            // Assert
            Assert.Equal(expectedDescriptor, descriptor, TagHelperDescriptorComparer.Default);
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string kind,
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null,
            IEnumerable<Action<TagMatchingRuleDescriptorBuilder>> ruleBuilders = null,
            Action<TagHelperDescriptorBuilder> configureAction = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(kind, typeName, assemblyName);
            builder.SetTypeName(typeName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BoundAttributeDescriptor(attributeBuilder);
                }
            }

            if (ruleBuilders != null)
            {
                foreach (var ruleBuilder in ruleBuilders)
                {
                    builder.TagMatchingRuleDescriptor(innerRuleBuilder => {
                        innerRuleBuilder.RequireTagName(tagName);
                        ruleBuilder(innerRuleBuilder);
                    });
                }
            }
            else
            {
                builder.TagMatchingRuleDescriptor(ruleBuilder => ruleBuilder.RequireTagName(tagName));
            }

            configureAction?.Invoke(builder);

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
