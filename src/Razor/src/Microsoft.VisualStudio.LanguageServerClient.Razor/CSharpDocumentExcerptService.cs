// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal sealed class CSharpDocumentExcerptService : DocumentExcerptServiceBase
    {
        private readonly IRazorSpanMappingService _mappingService;

        private readonly LSPDocumentSnapshot _documentSnapshot;

        public CSharpDocumentExcerptService(
            IRazorSpanMappingService mappingService,
            LSPDocumentSnapshot documentSnapshot)
        {
            if (mappingService is null)
            {
                throw new ArgumentNullException(nameof(mappingService));
            }

            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _mappingService = mappingService;
            _documentSnapshot = documentSnapshot;
        }

        // For testing use only
        internal CSharpDocumentExcerptService()
        {
        }

        internal override async Task<ExcerptResultInternal?> TryGetExcerptInternalAsync(
            Document document,
            TextSpan span,
            ExcerptModeInternal mode,
            CancellationToken cancellationToken)
        {
            var mappedSpans = await _mappingService.MapSpansAsync(document, new[] { span }, cancellationToken).ConfigureAwait(false);
            if (mappedSpans.Length == 0 || mappedSpans[0].Equals(default(RazorMappedSpanResult)))
            {
                return null;
            }

            return await TryGetExcerptInternalAsync(
                document,
                span,
                mode,
                _documentSnapshot.Snapshot.AsText(),
                mappedSpans[0].LinePositionSpan,
                cancellationToken).ConfigureAwait(false);
        }

        internal async Task<ExcerptResultInternal?> TryGetExcerptInternalAsync(
            Document document,
            TextSpan span,
            ExcerptModeInternal mode,
            SourceText razorDocumentText,
            LinePositionSpan mappedLinePosition,
            CancellationToken cancellationToken)
        {
            var razorDocumentSpan = razorDocumentText.Lines.GetTextSpan(mappedLinePosition);

            var generatedDocument = document;

            // First compute the range of text we want to we to display relative to the razor document.
            var excerptSpan = ChooseExcerptSpan(razorDocumentText, razorDocumentSpan, mode);

            // Then we'll classify the spans based on the razor document, since that's the coordinate
            // space that our output mappings use.
            var classifiedSpans = await ClassifyPreviewAsync(
                razorDocumentSpan,
                excerptSpan,
                span,
                generatedDocument,
                cancellationToken).ConfigureAwait(false);

            var excerptText = GetTranslatedExcerptText(razorDocumentText, ref razorDocumentSpan, ref excerptSpan, classifiedSpans);

            return new ExcerptResultInternal(excerptText, razorDocumentSpan, classifiedSpans.ToImmutable(), document, span);
        }

        private async Task<ImmutableArray<ClassifiedSpan>.Builder> ClassifyPreviewAsync(
            TextSpan razorSpan,
            TextSpan excerptSpan,
            TextSpan generatedSpan,
            Document generatedDocument,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<ClassifiedSpan>();

            var remainingSpan = excerptSpan;

            // We should be able to process this whole span as C#, so classify it.
            //
            // However, we'll have to translate it to the the generated document's coordinates to do that.
            var offsetRazorToGenerated = generatedSpan.Start - razorSpan.Start;
            var offsetExcerpt = new TextSpan(excerptSpan.Start + offsetRazorToGenerated, excerptSpan.Length);

            var classifiedSecondarySpans = await Classifier.GetClassifiedSpansAsync(
                generatedDocument,
                offsetExcerpt,
                cancellationToken);

            // Now we have to translate back to the razor document's coordinates.
            var offsetGeneratedToRazor = razorSpan.Start - generatedSpan.Start;
            foreach (var classifiedSecondarySpan in classifiedSecondarySpans)
            {
                // Ensure classified span is contained within our excerpt
                // Possible for FirstSpan.start & LastSpan.end to be out of range of the excerpt, but still intersecting
                if (classifiedSecondarySpan.TextSpan.Start + offsetGeneratedToRazor < excerptSpan.Start ||
                    classifiedSecondarySpan.TextSpan.End + offsetGeneratedToRazor > excerptSpan.End)
                {
                    continue;
                }

                var updated = new TextSpan(classifiedSecondarySpan.TextSpan.Start + offsetGeneratedToRazor, classifiedSecondarySpan.TextSpan.Length);

                // NOTE: The Classifier will only return spans for things that it understands. That means
                // that whitespace is not classified. The preview expects us to provide contiguous spans, 
                // so we are going to have to fill in the gaps.
                if (remainingSpan.Start < updated.Start)
                {
                    builder.Add(new ClassifiedSpan(
                        ClassificationTypeNames.Text,
                        new TextSpan(remainingSpan.Start, updated.Start - remainingSpan.Start)));
                    remainingSpan = new TextSpan(updated.Start, remainingSpan.Length - (updated.Start - remainingSpan.Start));
                }

                builder.Add(new ClassifiedSpan(classifiedSecondarySpan.ClassificationType, updated));
                remainingSpan = new TextSpan(updated.End, Math.Max(0, remainingSpan.Length - (updated.End - remainingSpan.Start)));
            }

            // Make sure that we're not introducing a gap. Remember, we need to fill in the whitespace.
            if (remainingSpan.Start < razorSpan.End)
            {
                builder.Add(new ClassifiedSpan(
                    ClassificationTypeNames.Text,
                    new TextSpan(remainingSpan.Start, razorSpan.End - remainingSpan.Start)));
                remainingSpan = new TextSpan(razorSpan.End, remainingSpan.Length - (razorSpan.End - remainingSpan.Start));
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
