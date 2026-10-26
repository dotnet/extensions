# Microsoft.Extensions.Documents.Abstractions

`Microsoft.Extensions.Documents.Abstractions` is an **experimental** and provider-neutral in-memory model for structured documents. It is deliberately independent of AI, DataIngestion, Markdig, provider SDKs, credentials, serialization libraries, and network APIs.

The model contains a document-owned ordered section hierarchy. Sections contain ordered `DocumentElement` instances, including nested sections, text elements (`Paragraph`, `Header`, or `Footer`), images, tables, and opaque extension elements. `Document.EnumerateContent()` provides depth-first traversal of non-section content.

## Intentional API boundaries

- `DocumentElement.StoredMarkdown` and `Document.StoredMarkdown` are stored source values. `GetMarkdown()` returns the same stored value, while `ToMarkdown()` is a separate semantic projection and does not overwrite or infer it.
- `LocalId` and `Address` are in-memory identity/address helpers. They are not durable serialization IDs, provider IDs, or source-grounding claims.
- Page references preserve opaque provider order and duplicates but do not assert that a page is authoritative source grounding.
- Evidence and extension values are opaque associations. Optional `DocumentAnnotation` instances preserve structured values, roles, source regions, reported confidence, and explicit native-observation access without adding provider SDK dependencies.
- `DocumentSourcePage` identifies the originating page and coordinate frame separately from assembly order. Dimensions, coordinate unit, and origin are optional: unknown values stay unknown. `DocumentRegion` copies every polygon vertex in producer order; it does not reduce rotated regions to rectangles. Confidence values retain their reported kind and optional scale and do not imply calibration across providers.
- Table cells are explicitly addressable and can contain any non-section element, including images, or retain an empty anchor with annotations. Positive row/column spans distinguish anchors from covered positions; `GetCoveringCell` and `IsCovered` expose that distinction. Overlapping/out-of-bounds spans, reuse, cycles, and invalid relationships are rejected. Simple tables retain their pipe-delimited row projection (without a GFM header/separator, so not a rendered GFM table); spans or producer roles use HTML to preserve those semantics. The simple projection's rendering policy remains an API-review consideration; it does not infer a header row or substitute potentially stale stored Markdown.
- `DocumentArtifact` owns its content stream, and an associated document disposes registered artifacts. The package does not fetch, create, or interpret artifacts.
- `Document.Version` tracks supported scalar, hierarchy, and artifact-collection edits. Changes to metadata, page-reference, evidence, annotation, and source-registry collections do not increment it.

## Source output and lifetime

`DocumentSourceOutput` represents one source operation. Register it explicitly or through an annotation/source page; multiple pages may reference the same instance. Operation identifiers identify one output instance within the document, and frame identifiers identify one frame instance within their operation.

`GetRawRepresentation()` exposes a borrowed native result for deliberate break-glass inspection, independently of response-content capture. `NativeStatus` is retained or unavailable; disabling content capture never disables native inspection. `GetRawContent()` exposes an owned artifact containing the original detached response body when available, **not** a reserialization of mapped SDK objects. `ContentStatus` distinguishes retained, unavailable, and explicitly disabled content capture. `DocumentSourceOutput` accepts native output even when `contentCaptureDisabled` is true, but rejects a response-content artifact in that case. Registered response artifacts are owned once and disposed with the document; borrowed native objects and structured values are not implicitly disposed. `DocumentArtifact.IsDocumentOwned` identifies an existing lifetime transfer, which a subsequent failed extraction must not dispose.

Annotations and raw outputs do not enter `Text` or Markdown projections automatically. There is no automatic logging, embedding, persistence, or wire serialization contract. Consumers must deliberately select which supporting data to inspect or persist, protect sensitive response content, and respect the document's lifetime. An empty stored Markdown value is valid for blank or image-only content; it does not fabricate text.

Attachment preflights subtree artifact ownership and annotation conflicts before
transferring ownership. Rejected scalar edits on disposed documents do not change
the retained value, and assigning the same section, element or artifact to its
current slot is a no-op while the document is alive. Disposal attempts every
registered artifact; one cleanup failure is rethrown, and multiple failures are
reported together in an `AggregateException`. Borrowed values are never disposed.

First-class list/item/nesting semantics and normalized barcode/language/link/email/address schemas remain separate API-review work. Structured supplied values can travel as annotations/extensions now; the package does not perform recognition, infer grounding, or define provider-specific feature flags.

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
