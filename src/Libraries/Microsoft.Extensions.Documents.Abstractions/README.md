# Microsoft.Extensions.Documents.Abstractions

`Microsoft.Extensions.Documents.Abstractions` is an **experimental**, provider-neutral in-memory model for structured documents. It provides a shared handoff so independently implemented producers and consumers can compose without depending on each other's SDKs or an ingestion framework. It is not an extraction engine or a durable interchange format.

Extraction adapters, application-created content, source viewers and ingestion integrations are examples of components that can use this boundary. The model is deliberately independent of AI, DataIngestion, Markdig, provider SDKs, credentials, serialization libraries and network APIs.

The model contains a document-owned ordered section hierarchy. Sections contain ordered `DocumentElement` instances, including nested sections, text, images and tables. Applications can derive their own element types when needed. `Document.EnumerateContent()` provides depth-first traversal of non-section content.

## Intentional API boundaries

- `DocumentElement.Markdown` and `Document.Markdown` hold supplied, mutable Markdown values. `ToMarkdown()` is a separate projection of the current structure and does not overwrite those values. Supplied Markdown is not an original response-body archive. Empty structures fall back to their supplied value; populated sections and tables project their children or cells.
- `DocumentTextElement.Role` is optional: unspecified text needs no classification. Common roles are `heading`, `pageHeader` and `pageFooter`; custom values are retained verbatim. Consumers should compare recognized roles case-insensitively and treat unrecognized roles as ordinary text. Repeated page headers are not document headings. Optional `HeadingLevel` is between 1 and 10; an absent level does not imply a hierarchy level.
- `Metadata` is an application-property bag on the document, not on every element. For example, an application can retain the original file's hash for citation identity. Provider-specific confidence, detections and transforms are not standardized by this model; native observations remain available through source references.
- `Sources` on an element or table cell contains `DocumentSourceReference` instances: a typed source page, an optional polygon and explicit borrowed native-observation access. A page-only reference has no polygon. References preserve order and duplicates; they do not verify content or establish durable citation identity.
- `DocumentSourcePage` identifies an originating page and coordinate frame separately from assembly order. Dimensions, coordinate unit and origin are optional: unknown values stay unknown. A source reference copies every supplied polygon vertex in order; it does not reduce rotated regions to rectangles.
- Table cells are constructed through the row/column indexer and can contain any non-section element, including images, or retain an empty anchor with sources. Positive row/column spans describe merged cells; covered positions do not contain a second anchor. Optional `Role` describes a cell classification such as `columnHeader` or `rowHeader`, not the content's CLR type. Overlapping/out-of-bounds spans, reuse, cycles, and invalid relationships are rejected. Simple tables retain their pipe-delimited row projection (without a GFM header/separator, so not a rendered GFM table); spans or producer roles use HTML to preserve those semantics. The simple projection's rendering policy remains an API-review consideration; it does not infer a header row or substitute potentially stale stored Markdown. This is a constructible exchange model, not a document editor.
- `DocumentArtifact` owns its content stream. Registering a `DocumentSourceOutput` with original response content transfers the artifact's lifetime to the document. Elements do not expose an additional artifact collection. The package does not fetch, create, or interpret artifacts.
- Presence, identity/address, mutation-version and disposal bookkeeping are not public contracts. Applications supply their own durable identifiers and revisions.

## Source output and lifetime

`DocumentSourceOutput` represents one source operation. Register it explicitly or through a source page/reference; multiple pages may reference the same instance. Operation identifiers identify one output instance within the document, and frame identifiers identify one frame instance within their operation. `SourcePages` and `SourceOutputs` inventory registered frames and operations, including blank pages with no content elements.

`GetRawRepresentation()` exposes a borrowed native result when supplied, or null, independently of response-content capture. Disabling content capture never disables native inspection and is not redaction of the native object. `GetRawContent()` exposes an owned artifact containing the original detached response body when available, **not** a reserialization of mapped SDK objects. `ContentStatus` distinguishes retained, unavailable and explicitly disabled content capture. `DocumentSourceOutput` accepts native output even when `contentCaptureDisabled` is true, but rejects a response-content artifact in that case. Registered response artifacts are owned once and disposed with the document; borrowed native objects and metadata values are not implicitly disposed. `DocumentArtifact.IsDocumentOwned` identifies an existing lifetime transfer, which a subsequent failed extraction must not dispose.

Source references, metadata and raw outputs do not enter `Text` or Markdown projections automatically. There is no automatic logging, embedding, persistence or wire serialization contract. Consumers must deliberately select which supporting data to inspect or persist, protect sensitive response content and respect the document's lifetime. An empty supplied Markdown value is valid for blank or image-only content; it does not fabricate text.

Attachment preflights subtree ownership and source conflicts, including response-body ownership, before
transferring ownership. Rejected scalar edits on disposed documents do not change
the retained value, and assigning the same section or element to its
current slot is a no-op while the document is alive. Disposal attempts every
registered artifact; one cleanup failure is rethrown, and multiple failures are
reported together in an `AggregateException`. Borrowed values are never disposed.

Generalized annotations/confidence, opaque evidence/page-reference wrappers, per-element metadata/artifact bags, a generic extension envelope, first-class list/item/nesting semantics and normalized barcode/language/link/email/address schemas are outside this first-pass surface. Provider-specific information remains available through native inspection; the package does not perform recognition, infer grounding or define provider-specific feature flags.

The API is subject to change during preview and API review. It is not a replacement for the existing `Microsoft.Extensions.DataIngestion` APIs in this slice.

The package's public API is marked experimental with diagnostic `EXTEXP0019`. Consuming applications can suppress that diagnostic explicitly while evaluating the package; it does not provide compatibility or serialization guarantees.

## Example

```csharp
using Microsoft.Extensions.Documents;

using Document document = new("invoice-42");
DocumentSection section = new("# Invoice");
section.Elements.Add(new DocumentTextElement("# Invoice", "Invoice", role: "heading") { HeadingLevel = 1 });
section.Elements.Add(new DocumentTextElement("Total: $10", "Total: $10"));
document.Sections.Add(section);

string markdown = document.ToMarkdown();
```
