using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using UglyToad.PdfPig;
using Microsoft.Extensions.AI;
using UglyToad.PdfPig.Content;

namespace ChatWithCustomData.Web.Services.Ingestion;

public class PDFDirectorySource(string sourceDirectory) : IIngestionSource
{
    public static string SourceFileId(string path) => Path.GetFileName(path);

    public string SourceId => $"{nameof(PDFDirectorySource)}:{sourceDirectory}";

    public async Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IQueryable<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = File.GetLastWriteTimeUtc(sourceFile).ToString("o");

            var existingDocument = await existingDocuments.Where(d => d.SourceId == SourceId && d.Id == sourceFileId).FirstOrDefaultAsync();
            if (existingDocument is null)
            {
                results.Add(new() { Id = sourceFileId, Version = sourceFileVersion, SourceId = SourceId });
            }
            else if (existingDocument.Version != sourceFileVersion)
            {
                existingDocument.Version = sourceFileVersion;
                results.Add(existingDocument);
            }
        }

        return results;
    }

    public async Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IQueryable<IngestedDocument> existingDocuments)
    {
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.pdf");
        var sourceFileIds = sourceFiles.Select(SourceFileId).ToList();
        return await existingDocuments
            .Where(d => !sourceFileIds.Contains(d.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<SemanticSearchRecord>> CreateRecordsForDocumentAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, string documentId)
    {
        using var pdf = PdfDocument.Open(Path.Combine(sourceDirectory, documentId));
        var paragraphs = pdf.GetPages().SelectMany(GetPageParagraphs).ToList();
        var embeddings = await embeddingGenerator.GenerateAsync(paragraphs.Select(c => c.Text));

        return paragraphs.Zip(embeddings).Select((pair, index) => new SemanticSearchRecord
        {
            Key = $"{Path.GetFileNameWithoutExtension(documentId)}_{pair.First.PageNumber}_{pair.First.IndexOnPage}",
            FileName = documentId,
            PageNumber = pair.First.PageNumber,
            Text = pair.First.Text,
            Vector = pair.Second.Vector,
        });
    }

    private static IEnumerable<(int PageNumber, int IndexOnPage, string Text)> GetPageParagraphs(Page pdfPage)
    {
        var letters = pdfPage.Letters;
        var words = NearestNeighbourWordExtractor.Instance.GetWords(letters);
        var textBlocks = DocstrumBoundingBoxes.Instance.GetBlocks(words);
        var pageText = string.Join(Environment.NewLine + Environment.NewLine,
            textBlocks.Select(t => t.Text.ReplaceLineEndings(" ")));

#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        return TextChunker.SplitPlainTextParagraphs([pageText], 200)
            .Select((text, index) => (pdfPage.Number, index, text));
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only
    }
}
