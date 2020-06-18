// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class RazorDocumentExcerptService : DocumentExcerptServiceBase
    {
        private readonly DocumentSnapshot _document;
        private readonly IRazorSpanMappingService _mappingService;

        public RazorDocumentExcerptService(DocumentSnapshot document, IRazorSpanMappingService mappingService)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (mappingService is null)
            {
                throw new ArgumentNullException(nameof(mappingService));
            }

            _document = document;
            _mappingService = mappingService;
        }

        internal override async Task<ExcerptResultInternal?> TryGetExcerptInternalAsync(
            Document document,
            TextSpan span,
            ExcerptModeInternal mode,
            CancellationToken cancellationToken)
        { 
            if (_document is null)
            {
                return null;
            }

            var mappedSpans = await _mappingService.MapSpansAsync(document, new[] { span }, cancellationToken).ConfigureAwait(false);
            if (mappedSpans.Length == 0 || mappedSpans[0].Equals(default(RazorMappedSpanResult)))
            {
                return null;
            }

            var project = _document.Project;
            var razorDocument = project.GetDocument(mappedSpans[0].FilePath);
            if (razorDocument is null)
            {
                return null;
            }

            var razorDocumentText = await razorDocument.GetTextAsync().ConfigureAwait(false);
            var razorDocumentSpan = razorDocumentText.Lines.GetTextSpan(mappedSpans[0].LinePositionSpan);

            var generatedDocument = document;

            // First compute the range of text we want to we to display relative to the primary document.
            var excerptSpan = ChooseExcerptSpan(razorDocumentText, razorDocumentSpan, mode);

            // Then we'll classify the spans based on the primary document, since that's the coordinate
            // space that our output mappings use.
            var output = await _document.GetGeneratedOutputAsync().ConfigureAwait(false);
            var mappings = output.GetCSharpDocument().SourceMappings;
            var classifiedSpans = await ClassifyPreviewAsync(
                excerptSpan, 
                generatedDocument, 
                mappings,
                cancellationToken).ConfigureAwait(false);


            var excerptText = GetTranslatedExcerptText(razorDocumentText, ref razorDocumentSpan, ref excerptSpan, classifiedSpans);

            return new ExcerptResultInternal(excerptText, razorDocumentSpan, classifiedSpans.ToImmutable(), document, span);
        }

        private async Task<ImmutableArray<ClassifiedSpan>.Builder> ClassifyPreviewAsync(
            TextSpan excerptSpan,
            Document generatedDocument,
            IReadOnlyList<SourceMapping> mappings,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();

            var sorted = new List<SourceMapping>(mappings);
            sorted.Sort((x, y) => x.OriginalSpan.AbsoluteIndex.CompareTo(y.OriginalSpan.AbsoluteIndex));

            // The algorithm here is to iterate through the source mappings (sorted) and use the C# classifier
            // on the spans that are known to be C#. For the spans that are not known to be C# then 
            // we just treat them as text since we'd don't currently have our own classifications.

            var remainingSpan = excerptSpan;
            for (var i = 0; i < sorted.Count && excerptSpan.Length > 0; i++)
            {
                var primarySpan = sorted[i].OriginalSpan.AsTextSpan();
                var intersection = primarySpan.Intersection(remainingSpan);
                if (intersection == null)
                {
                    // This span is outside the area we're interested in.
                    continue;
                }

                // OK this span intersects with the excerpt span, so we will process it. Let's compute
                // the secondary span that matches the intersection.
                var secondarySpan = sorted[i].GeneratedSpan.AsTextSpan();
                secondarySpan = new TextSpan(secondarySpan.Start + intersection.Value.Start - primarySpan.Start, intersection.Value.Length);
                primarySpan = intersection.Value;
                
                if (remainingSpan.Start < primarySpan.Start)
                {
                    // The position is before the next C# span. Classify everything up to the C# start
                    // as text.
                    builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, new TextSpan(remainingSpan.Start, primarySpan.Start - remainingSpan.Start)));

                    // Advance to the start of the C# span.
                    remainingSpan = new TextSpan(primarySpan.Start, remainingSpan.Length - (primarySpan.Start - remainingSpan.Start));
                }

                // We should be able to process this whole span as C#, so classify it.
                //
                // However, we'll have to translate it to the the generated document's coordinates to do that.
                Debug.Assert(remainingSpan.Contains(primarySpan) && remainingSpan.Start == primarySpan.Start);
                var classifiedSecondarySpans = await Classifier.GetClassifiedSpansAsync(
                    generatedDocument, 
                    secondarySpan, 
                    cancellationToken);

                // NOTE: The Classifier will only returns spans for things that it understands. That means
                // that whitespace is not classified. The preview expects us to provide contiguous spans, 
                // so we are going to have to fill in the gaps.
                
                // Now we have to translate back to the primary document's coordinates.
                var offset = primarySpan.Start - secondarySpan.Start;
                foreach (var classifiedSecondarySpan in classifiedSecondarySpans)
                {
                    Debug.Assert(secondarySpan.Contains(classifiedSecondarySpan.TextSpan));
                    
                    var updated = new TextSpan(classifiedSecondarySpan.TextSpan.Start + offset, classifiedSecondarySpan.TextSpan.Length);
                    Debug.Assert(primarySpan.Contains(updated));

                    // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
                    if (remainingSpan.Start < updated.Start)
                    {
                        builder.Add(new ClassifiedSpan(
                            ClassificationTypeNames.Text,
                            new TextSpan(remainingSpan.Start, updated.Start - remainingSpan.Start)));
                        remainingSpan = new TextSpan(updated.Start, remainingSpan.Length - (updated.Start - remainingSpan.Start));
                    }
                    
                    builder.Add(new ClassifiedSpan(classifiedSecondarySpan.ClassificationType, updated));
                    remainingSpan = new TextSpan(updated.End, remainingSpan.Length - (updated.End - remainingSpan.Start));
                }

                // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
                if (remainingSpan.Start < primarySpan.End)
                {
                    builder.Add(new ClassifiedSpan(
                        ClassificationTypeNames.Text,
                        new TextSpan(remainingSpan.Start, primarySpan.End - remainingSpan.Start)));
                    remainingSpan = new TextSpan(primarySpan.End, remainingSpan.Length - (primarySpan.End - remainingSpan.Start));
                }

            }

            // Deal with residue
            if (remainingSpan.Length > 0)
            {
                // Trailing Razor/markup text.
                builder.Add(new ClassifiedSpan(ClassificationTypeNames.Text, remainingSpan));
            }

            return builder;
        }
    }
}
