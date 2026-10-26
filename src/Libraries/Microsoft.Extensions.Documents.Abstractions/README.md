# Microsoft.Extensions.Documents.Abstractions

`Microsoft.Extensions.Documents.Abstractions` is an **experimental**, provider-neutral in-memory model for structured documents. It provides a shared handoff so independently implemented producers and consumers can compose without depending on each other's SDKs or an ingestion framework. It is not an extraction engine or a durable interchange format.

Extraction adapters, application-created content, source viewers and ingestion integrations are examples of components that can use this boundary. The model is deliberately independent of AI, DataIngestion, Markdig, provider SDKs, credentials, serialization libraries and network APIs.

The model contains a document-owned ordered section hierarchy. Sections contain ordered `DocumentElement` instances, including nested sections, text elements (`Paragraph`, `Header`, or `Footer`), images, tables, and opaque extension elements. `Document.EnumerateContent()` provides depth-first traversal of non-section content.

## Intentional API boundaries

- `DocumentElement.StoredMarkdown` and `Document.StoredMarkdown` are stored source values. `GetMarkdown()` returns the same stored value, while `ToMarkdown()` is a separate semantic projection and does not overwrite or infer it.
- `Metadata` is the extension-property bag on documents and elements. Provider-specific confidence, detections, transforms and workflow data are not standardized by this first-pass model. Opaque extension elements can carry nonstandard content.
- `Sources` on an element or table cell contains `DocumentSourceReference` instances: a typed source page, an optional polygon and explicit borrowed native-observation access. A page-only reference has no polygon. References preserve order and duplicates; they do not verify content or establish durable citation identity.
- `DocumentSourcePage` identifies an originating page and coordinate frame separately from assembly order. Dimensions, coordinate unit and origin are optional: unknown values stay unknown. A source reference copies every supplied polygon vertex in order; it does not reduce rotated regions to rectangles.
- Table cells are explicitly addressable and can contain any non-section element, including images, or retain an empty anchor with sources. Positive row/column spans distinguish anchors from covered positions; `GetCoveringCell` and `IsCovered` expose that distinction. Overlapping/out-of-bounds spans, reuse, cycles, and invalid relationships are rejected. Simple tables retain their pipe-delimited row projection (without a GFM header/separator, so not a rendered GFM table); spans or producer roles use HTML to preserve those semantics. The simple projection's rendering policy remains an API-review consideration; it does not infer a header row or substitute potentially stale stored Markdown.
- `DocumentArtifact` owns its content stream, and an associated document disposes registered artifacts. The package does not fetch, create, or interpret artifacts.
- Presence, identity/address, mutation-version and disposal bookkeeping are not public contracts. Applications supply their own durable identifiers and revisions.

## Source output and lifetime

`DocumentSourceOutput` represents one source operation. Register it explicitly or through a source page/reference; multiple pages may reference the same instance. Operation identifiers identify one output instance within the document, and frame identifiers identify one frame instance within their operation. `SourcePages` and `SourceOutputs` inventory registered frames and operations, including blank pages with no content elements.

`GetRawRepresentation()` exposes a borrowed native result when supplied, or null, independently of response-content capture. Disabling content capture never disables native inspection and is not redaction of the native object. `GetRawContent()` exposes an owned artifact containing the original detached response body when available, **not** a reserialization of mapped SDK objects. `ContentStatus` distinguishes retained, unavailable and explicitly disabled content capture. `DocumentSourceOutput` accepts native output even when `contentCaptureDisabled` is true, but rejects a response-content artifact in that case. Registered response artifacts are owned once and disposed with the document; borrowed native objects and extension values are not implicitly disposed. `DocumentArtifact.IsDocumentOwned` identifies an existing lifetime transfer, which a subsequent failed extraction must not dispose.

Source references, extension data and raw outputs do not enter `Text` or Markdown projections automatically. There is no automatic logging, embedding, persistence or wire serialization contract. Consumers must deliberately select which supporting data to inspect or persist, protect sensitive response content and respect the document's lifetime. An empty stored Markdown value is valid for blank or image-only content; it does not fabricate text.

Attachment preflights subtree artifact ownership and source conflicts before
transferring ownership. Rejected scalar edits on disposed documents do not change
the retained value, and assigning the same section, element or artifact to its
current slot is a no-op while the document is alive. Disposal attempts every
registered artifact; one cleanup failure is rethrown, and multiple failures are
reported together in an `AggregateException`. Borrowed values are never disposed.

Generalized annotations/confidence, opaque evidence/page-reference wrappers, first-class list/item/nesting semantics and normalized barcode/language/link/email/address schemas are outside this first-pass surface. Provider-specific information remains available through native inspection or extension data; the package does not perform recognition, infer grounding or define provider-specific feature flags.

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
