// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentRegistrationScalingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope",
        Justification = "Registration transfers artifacts to the using-scoped document; disposal is asserted.")]
    public void RegisterSource_DoesNotRevisitUnrelatedRawStreams(int sourceCount)
    {
        using Document document = new("registration");
        DocumentSection section = new("");
        document.Sections.Add(section);
        CapabilityProbeStream[] streams = new CapabilityProbeStream[sourceCount];
        DocumentSourceOutput[] sources = new DocumentSourceOutput[sourceCount];
        for (int index = 0; index < sourceCount; index++)
        {
            streams[index] = new CapabilityProbeStream();
            sources[index] = new DocumentSourceOutput($"source-{index}",
                rawContent: new DocumentArtifact("application/json", streams[index]));
            DocumentSourcePage page = new($"page-{index}", index + 1, sourceOutput: sources[index]);
            int[] before = streams.Take(index).Select(static stream => stream.CapabilityReads).ToArray();

            section.Sources.Add(new DocumentSourceReference(page, DocumentAnnotationTestData.Polygon()));

            Assert.Equal(before, streams.Take(index).Select(static stream => stream.CapabilityReads));
            Assert.Same(sources[index], document.SourceOutputs[index]);
            Assert.Same(page, document.SourcePages[index]);
        }

        int[] registeredReads = streams.Select(static stream => stream.CapabilityReads).ToArray();
        section.Elements.Add(new DocumentTextElement("application-only"));
        Assert.Equal(registeredReads, streams.Select(static stream => stream.CapabilityReads));
        Assert.Equal(sourceCount, document.SourceOutputs.Count);
        Assert.Equal(sourceCount, document.SourcePages.Count);
        Assert.Equal(sourceCount, document.Artifacts.Count);
        Assert.Equal("application-only", document.ToMarkdown());
        Assert.All(sources, static source => Assert.True(source.GetRawContent()!.IsDocumentOwned));
        document.Dispose();
        Assert.All(streams, static stream => Assert.Equal(1, stream.DisposeCount));
    }

    private sealed class CapabilityProbeStream : MemoryStream
    {
        internal int CapabilityReads { get; private set; }
        internal int DisposeCount { get; private set; }

        public override bool CanRead
        {
            get
            {
                CapabilityReads++;
                return base.CanRead;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeCount++;
            }

            base.Dispose(disposing);
        }
    }
}
