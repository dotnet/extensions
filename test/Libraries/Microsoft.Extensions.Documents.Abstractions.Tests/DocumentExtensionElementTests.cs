// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentExtensionElementTests
{
    [Fact]
    public void Cleanup_RemovesObsoletePublicTypesAndMembers()
    {
        var assembly = typeof(Document).Assembly;
        foreach (string name in new[] { "DocumentArtifactCollection", "DocumentExtensionElement", "DocumentTextKind" })
        {
            Assert.Null(assembly.GetType("Microsoft.Extensions.Documents." + name));
        }

        foreach (Type type in new[]
        {
            typeof(Document), typeof(DocumentElement), typeof(DocumentSection),
            typeof(DocumentTextElement), typeof(DocumentImageElement), typeof(DocumentTableElement)
        })
        {
            Assert.Null(type.GetProperty("StoredMarkdown"));
            Assert.Null(type.GetMethod("GetMarkdown"));
            Assert.Equal(typeof(string), type.GetProperty("Markdown")!.PropertyType);
        }

        foreach (string name in new[] { "Metadata", "HasMetadata", "Artifacts" })
        {
            Assert.Null(typeof(DocumentElement).GetProperty(name));
            Assert.Null(typeof(DocumentElement).GetProperty(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        }

        Assert.Null(typeof(DocumentTextElement).GetProperty("HeaderLevel"));
        Assert.Null(typeof(DocumentTextElement).GetProperty("Kind"));
        Assert.Equal(typeof(int?), typeof(DocumentTextElement).GetProperty("HeadingLevel")!.PropertyType);
        Assert.Equal(typeof(string), typeof(DocumentTextElement).GetProperty("Role")!.PropertyType);
        Assert.False(typeof(DocumentTextElement).GetProperty("Role")!.CanWrite);
        var parameters = Assert.Single(typeof(DocumentTextElement).GetConstructors()).GetParameters();
        Assert.Equal(3, parameters.Length);
        Assert.Equal("markdown", parameters[0].Name);
        Assert.Equal("text", parameters[1].Name);
        Assert.Equal("role", parameters[2].Name);
        Assert.All(parameters, parameter => Assert.Equal(typeof(string), parameter.ParameterType));
        Assert.False(parameters[0].IsOptional);
        Assert.True(parameters[1].IsOptional);
        Assert.True(parameters[2].IsOptional);
        Assert.Null(parameters[1].DefaultValue);
        Assert.Null(parameters[2].DefaultValue);
        Assert.Equal(typeof(System.Collections.Generic.IDictionary<string, object>),
            typeof(Document).GetProperty("Metadata")!.PropertyType);
        Assert.Null(typeof(Document).GetProperty("Artifacts"));
    }

    [Fact]
    public void RootMetadataAndSources_PreserveOpaqueNestedPayloadAndNativeObservations()
    {
        using Document document = new("payload");
        using DocumentAnnotationTestData.BorrowedNative nestedNative = new();
        var payload = new { kind = "vendor-extra", nested = new { unknown = new object[] { 3, "x" }, native = nestedNative } };
        var attributes = new { vendorFlag = "kept", provenance = "urn:synthetic:engine:v1", native = nestedNative };
        DocumentTextElement extension = new("visible **text**", "visible text", "vendor-extra");
        DocumentSourceOutput output = new("operation-A", payload, providerName: "synthetic", modelId: "engine:v1");
        DocumentSourceReference first = new(DocumentAnnotationTestData.PdfPage(output), DocumentAnnotationTestData.Polygon(), attributes);
        DocumentSourceReference second = new(new DocumentSourcePage("frame-2", 2, sourceOutput: output), DocumentAnnotationTestData.Polygon());
        extension.Sources.Add(first);
        extension.Sources.Add(second);
        Assert.False(document.HasMetadata);
        document.Metadata["vendor-extra"] = payload;
        document.Metadata["vendorFlag"] = "kept";
        DocumentSection section = new("");
        section.Elements.Add(extension);
        document.Sections.Add(section);

        Assert.True(document.HasMetadata);
        Assert.Same(payload, document.Metadata["vendor-extra"]);
        Assert.Equal("vendor-extra", extension.Role);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Same(attributes, first.GetRawRepresentation());
        Assert.Equal("kept", attributes.vendorFlag);
        Assert.Equal("urn:synthetic:engine:v1", attributes.provenance);
        Assert.Equal(new[] { first, second }, extension.Sources);
        Assert.Equal(new[] { first.Page, second.Page }, document.SourcePages);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(extension, Assert.Single(document.EnumerateContent()));
        Assert.Equal("kept", document.Metadata["vendorFlag"]);
        Assert.Equal("visible **text**", document.ToMarkdown());
        Assert.Equal("visible text", extension.Text);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<DocumentPoint>)first.Polygon).Clear());
        document.Dispose();
        document.Dispose();
        Assert.Same(payload, output.GetRawRepresentation());
        Assert.Same(attributes, first.GetRawRepresentation());
        Assert.Same(nestedNative, payload.nested.native);
        Assert.Same(nestedNative, attributes.native);
        Assert.Equal(0, nestedNative.DisposeCount);
    }
}
