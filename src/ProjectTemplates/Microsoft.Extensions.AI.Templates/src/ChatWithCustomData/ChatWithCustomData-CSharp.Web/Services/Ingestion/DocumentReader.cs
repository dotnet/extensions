using Microsoft.Extensions.DataIngestion;

namespace ChatWithCustomData_CSharp.Web.Services.Ingestion;

internal sealed class DocumentReader : IngestionDocumentReader
{
    private readonly MarkdownReader _markdownReader = new();
    private readonly MarkItDownReader _markItDownReader = new();

    public override Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
        => base.ReadAsync(source, identifier, mediaType: GetCustomMediaType(source) ?? mediaType, cancellationToken);

    public override Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
        => mediaType switch
        {
            "application/pdf" => _markItDownReader.ReadAsync(source, identifier, mediaType, cancellationToken),
            "text/markdown" => _markdownReader.ReadAsync(source, identifier, mediaType, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported media type '{mediaType}'"),
        };

    private static string? GetCustomMediaType(FileInfo source)
        => source.Extension switch
        {
            ".md" => "text/markdown",
            _ => null
        };
}
