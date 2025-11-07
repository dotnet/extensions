using Microsoft.Extensions.DataIngestion;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace aichatweb.Services.Ingestion;

internal sealed class PdfPigReader : IngestionDocumentReader
{
    public override Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        using var pdf = PdfDocument.Open(source);
        var document = new IngestionDocument(identifier);
        foreach (var section in pdf.GetPages().SelectMany(GetPageSections))
        {
            document.Sections.Add(section);
        }
        return Task.FromResult(document);
    }

    private static IEnumerable<IngestionDocumentSection> GetPageSections(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select(text => new IngestionDocumentSection(text)
            {
                Text = text,
                Elements = { new IngestionDocumentParagraph(text) },
                PageNumber = pdfPage.Number,
            });
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}
