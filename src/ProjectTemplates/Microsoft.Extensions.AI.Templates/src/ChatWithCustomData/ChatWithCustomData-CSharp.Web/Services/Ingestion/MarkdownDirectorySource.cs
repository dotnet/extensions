using Microsoft.Extensions.DataIngestion;

namespace ChatWithCustomData_CSharp.Web.Services.Ingestion;

public class MarkdownDirectorySource(string sourceDirectory) : IIngestionSource
{
    public static string SourceFileId(string path) => Path.GetFileName(path);
    public static string SourceFileVersion(string path) => File.GetLastWriteTimeUtc(path).ToString("o");

    public string SourceId => $"{nameof(MarkdownDirectorySource)}:{sourceDirectory}";

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md");
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = SourceFileVersion(sourceFile);
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentVersion != sourceFileVersion)
            {
#if (IsQdrant)
                results.Add(new() { Key = Guid.CreateVersion7(), SourceId = SourceId, DocumentId = sourceFileId, DocumentVersion = sourceFileVersion });
#else
                results.Add(new() { Key = Guid.CreateVersion7().ToString(), SourceId = SourceId, DocumentId = sourceFileId, DocumentVersion = sourceFileVersion });
#endif
            }
        }

        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentFiles = Directory.GetFiles(sourceDirectory, "*.md");
        var currentFileIds = currentFiles.ToLookup(SourceFileId);
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    public async Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var reader = new MarkdownReader();
        var fileInfo = new FileInfo(Path.Combine(sourceDirectory, document.DocumentId));
        var ingestionDocument = await reader.ReadAsync(fileInfo);

        var chunks = ingestionDocument.EnumerateContent().Select(s => new IngestedChunk
        {
#if (IsQdrant)
            Key = Guid.CreateVersion7(),
#else
            Key = Guid.CreateVersion7().ToString(),
#endif
            DocumentId = document.DocumentId,
            PageNumber = s.PageNumber ?? 0,
            Text = s.Text ?? string.Empty,
        });

        return chunks;
    }
}
