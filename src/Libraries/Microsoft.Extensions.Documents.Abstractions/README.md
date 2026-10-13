# Microsoft.Extensions.Documents.Abstractions

`Microsoft.Extensions.Documents.Abstractions` is an **experimental** and provider-neutral in-memory model for structured documents. It is deliberately independent of AI, DataIngestion, Markdig, provider SDKs, credentials, serialization libraries, and network APIs.

The model contains a document-owned ordered section hierarchy. Sections contain ordered `DocumentElement` instances, including nested sections, text elements (`Paragraph`, `Header`, or `Footer`), images, tables, and opaque extension elements. `Document.EnumerateContent()` provides depth-first traversal of non-section content.

## Intentional API boundaries

- `DocumentElement.StoredMarkdown` and `Document.StoredMarkdown` are stored source values. `GetMarkdown()` returns the same stored value, while `ToMarkdown()` is a separate semantic projection and does not overwrite or infer it.
- `LocalId` and `Address` are in-memory identity/address helpers. They are not durable serialization IDs, provider IDs, or source-grounding claims.
- Page references preserve opaque provider order and duplicates but do not assert that a page is authoritative source grounding.
- Evidence and extension values are opaque associations. The package does not define provider-specific geometry, confidence, continuation, or extraction types.
- Table cells are explicitly addressable and can contain any non-section element, including images. Ownership rules reject reuse, cycles, and invalid table relationships.
- `DocumentArtifact` owns its content stream, and an associated document disposes registered artifacts. The package does not fetch, create, or interpret artifacts.
- `Document.Version` tracks supported scalar, hierarchy, and artifact-collection edits. Changes to metadata, page-reference, and evidence collections do not increment it.

The API is subject to change during preview and API review. It is not a replacement for the existing `Microsoft.Extensions.DataIngestion` APIs in this slice.

The package's public API is marked experimental with diagnostic `EXTEXP0019`. Consuming applications can suppress that diagnostic explicitly while evaluating the package; it does not provide compatibility or serialization guarantees.

## Example

```csharp
using Microsoft.Extensions.Documents;

using Document document = new("invoice-42");
DocumentSection section = new("# Invoice");
section.Elements.Add(new DocumentTextElement(DocumentTextKind.Header, "# Invoice", "Invoice"));
section.Elements.Add(new DocumentTextElement(DocumentTextKind.Paragraph, "Total: $10", "Total: $10"));
document.Sections.Add(section);

string markdown = document.ToMarkdown();
```
