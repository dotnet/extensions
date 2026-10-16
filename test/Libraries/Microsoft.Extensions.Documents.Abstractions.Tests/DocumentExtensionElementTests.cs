// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentExtensionElementTests
{
    [Fact]
    public void PreservesOpaqueTypeAndValue()
    {
        object value = new();
        DocumentExtensionElement element = new("provider-extension", "stored extension", value);

        Assert.Equal("provider-extension", element.Type);
        Assert.Equal("stored extension", element.GetMarkdown());
        Assert.Same(value, element.Value);
    }

    [Fact]
    public void RejectsNullType()
    {
        Assert.Throws<ArgumentNullException>(
            "type",
            () => new DocumentExtensionElement(null!, "stored extension"));
    }

    [Fact]
    public void RejectsEmptyType()
    {
        Assert.Throws<ArgumentException>(
            "type",
            () => new DocumentExtensionElement("", "stored extension"));
    }

    [Fact]
    public void ExtensionAndAnnotations_PreserveOpaqueNestedPayloadAttributesAndRoles()
    {
        using Document document = new("payload");
        var payload = new { kind = "vendor-extra", nested = new { unknown = new object[] { 3, "x" } } };
        var attributes = new { vendorFlag = "kept", provenance = "urn:synthetic:engine:v1" };
        DocumentExtensionElement extension = new("vendor-extra", "visible **text**", payload) { Text = "visible text" };
        DocumentSourceOutput output = new("operation-A", providerName: "synthetic", modelId: "engine:v1");
        DocumentRegion first = new(DocumentAnnotationTestData.PdfPage(output), DocumentAnnotationTestData.Polygon());
        DocumentRegion second = new(new DocumentSourcePage("frame-2", 2, sourceOutput: output), DocumentAnnotationTestData.Polygon());
        DocumentRegion[] supplied = [first, second];
        DocumentAnnotation annotation = new("vendor-observation", attributes, "caption", supplied, sourceOutput: output);
        supplied[0] = second;
        extension.Annotations.Add(annotation);
        extension.Metadata["vendorFlag"] = "kept";
        DocumentSection section = new("");
        section.Elements.Add(extension);
        document.Sections.Add(section);

        Assert.Same(payload, extension.Value);
        Assert.Equal(new object[] { 3, "x" }, payload.nested.unknown);
        Assert.Same(attributes, annotation.Value);
        Assert.Equal("kept", attributes.vendorFlag);
        Assert.Equal("urn:synthetic:engine:v1", attributes.provenance);
        Assert.Equal("caption", annotation.Role);
        Assert.Equal(new[] { first, second }, annotation.Regions);
        Assert.Equal(new[] { first.Page, second.Page }, document.SourcePages);
        Assert.Same(output, Assert.Single(document.SourceOutputs));
        Assert.Same(extension, Assert.Single(document.EnumerateContent()));
        Assert.Equal("kept", extension.Metadata["vendorFlag"]);
        Assert.Equal("visible **text**", document.ToMarkdown());
        Assert.Equal("visible text", extension.Text);
        Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IList<DocumentRegion>)annotation.Regions).Clear());
    }
}
