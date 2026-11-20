# Microsoft.Extensions.Documents.Abstractions

`Microsoft.Extensions.Documents.Abstractions` is an **experimental**, provider-neutral in-memory model for structured documents. It provides a shared handoff so independently implemented producers and consumers can compose without depending on each other's SDKs or an ingestion framework. It is not an extraction engine or a durable interchange format.

Extraction adapters, application-created content, source viewers and ingestion integrations are examples of components that can use this boundary. Payloads use the standard `AIContent` types from `Microsoft.Extensions.AI.Abstractions`. The model does not depend on DataIngestion, Markdig, provider SDKs, credentials or network calls.

The model contains an ordered section hierarchy. Sections contain ordered `ExtractionDocumentElement` instances, including nested sections, text, images and tables. Applications can derive their own element types when needed. Producers choose the hierarchy shape: one producer can emit flat page sections while another nests sections by heading, so consumers should not assume a fixed depth (see [Reading a document](#reading-a-document)). This package has no structural renderer such as Markdown or HTML output; `GetText()` is a plain-text projection.

## Reading a document

Text can be in text elements at any section depth, table cells (whose content can itself be a section), image `Text`, and the local `Content` of the document, sections and tables. A loop that only visits `document.Sections` and their direct `Elements`, or that only keeps `ExtractionDocumentTextElement` instances, still compiles but silently drops some of that text, and how much it drops depends on the producer.

- Use `document.GetText()` to get all text, in document order. It includes page headers/footers, table cells and image `Text`, and excludes `AlternativeText`, bytes and references.
- Use `document.EnumerateContent()` (or `section.EnumerateContent()`) with `element.GetText()` to filter or process elements individually. `EnumerateContent()` descends nested sections and returns tables and images as elements, so call `GetText()` on every element instead of keeping only text elements. It does not return document/section-local payloads.
- Walk `Sections` and `Elements` recursively when you need the hierarchy. Handle nested `ExtractionDocumentSection` elements, read table content through `table.Cells` (which enumerates anchor cells), and use `GetCoveringCell` to resolve merged positions.

```csharp
foreach (ExtractionDocumentElement element in document.EnumerateContent())
{
    if (element is ExtractionDocumentTextElement { Role: { } role } &&
        (role.Equals("pageHeader", StringComparison.OrdinalIgnoreCase) ||
         role.Equals("pageFooter", StringComparison.OrdinalIgnoreCase)))
    {
        continue;
    }

    string text = element.GetText();
    if (text.Length > 0)
    {
        Console.WriteLine(text);
    }
}
```

## Intentional API boundaries

- `ExtractionDocumentElement.Content` and `ExtractionDocument.Content` hold optional **local** `AIContent` payloads. Use `TextContent` for text, `DataContent` for bytes with MIME, and `UriContent` for references with MIME. Structural containers can have null content. Do not populate ancestors with aggregate copies of their descendants.
- `TextContent.Text` is the sole authoritative text for text elements. Plain text and producer-supplied formatted text are preserved without conversion or a required Markdown convention. There is no duplicate general plain-text property.
- `ExtractionDocumentImageElement.Content` is inherited from `ExtractionDocumentElement`, not a second byte-valued slot. MIME is supplied by `DataContent` or `UriContent`; there is no separate mutable `MediaType` to disagree with it. `Source` is the original asset reference, independent of any `UriContent` payload URI, and can coexist with bytes. Image `Text` retains OCR/transcription independently of payload and `AlternativeText`. Captions and descriptions belong in `AlternativeText`, not duplicated into OCR. No URI is fetched or fabricated.
- `ExtractionDocumentTextElement.Role` is optional: unspecified text needs no classification. Common roles are `heading`, `pageHeader` and `pageFooter`; custom values are retained verbatim. Consumers should compare recognized roles case-insensitively and treat unrecognized roles as ordinary text. Repeated page headers are not document headings. Optional `HeadingLevel` is positive when supplied; an absent level does not imply a hierarchy level.
- `Metadata` is an application-property bag on the document, not on every element. For example, an application can retain the original file's hash for citation identity. Provider-specific confidence, detections and transforms are not standardized by this model; native observations remain available through source references.
- `Sources` on an element or table cell contains `ExtractionDocumentSourceReference` instances: a typed source page, an optional polygon and explicit borrowed native-observation access. A page-only reference has no polygon. References preserve order and duplicates; they do not verify content or establish durable citation identity.
- `ExtractionDocumentSourcePage` identifies an originating page and coordinate frame separately from assembly order. `PageNumber` is nullable and positive when supplied; named or unnumbered frames keep their identity without fabricating a number. Dimensions, coordinate unit and origin are optional: unknown values stay unknown. Default dimensions and explicit `(0,0)` dimensions are the same empty value, but source pages reject empty dimensions and use `null` for unknown size. Nonempty dimensions must be finite and positive in both axes. Geometry uses explicit readonly value structs with coordinate-based equality. A source reference snapshots any supplied vertex sequence once in order; it does not reduce rotated regions to rectangles.
- Table cells are constructed through the row/column indexer and can contain an element, including an image or a section with ordered mixed content, or retain an empty anchor with sources. Use a section's `Elements` for multiple paragraphs and images in one cell; its local `Content` is not an aggregate copy. Positive row/column spans describe merged cells; covered positions do not contain a second anchor. The indexer returns `null` for both empty and covered positions; edit a covered position through its anchor, and use `GetCoveringCell` to find the anchor covering any position. Enumerating `Cells` yields only explicit anchor cells, in row-major order. Optional `Role` describes a cell classification such as `columnHeader` or `rowHeader`, not the content's CLR type. The grid rejects overlapping/out-of-bounds spans and reuse of a cell instance at multiple addresses. This is a constructible exchange model, not a document editor.
- Mutable collections use ordinary `List<T>` and `Dictionary<TKey, TValue>` properties. Composition does not attach elements to a parent, register sources, or track mutation versions. Elements can be shared. Structural traversal rejects null entries and cyclic section paths while returning every occurrence of shared acyclic content. `EnumerateContent()` does not recurse into table cells; the typed grid validates its own cell coordinates and spans. `GetText()` reads table cells and rejects cyclic section or table paths. Consumers performing additional recursive traversal must validate their active paths.
- Applications supply their own durable identifiers and revisions; the model has no editing-framework ownership, address, mutation-version or disposal bookkeeping.

## Native inspection and lifetime

`SourcePages` is an ordinary mutable list. Producers explicitly add frames with `document.SourcePages.Add(frame)`, including blank frames with no content elements. Adding elements or source references does not populate the list. Assembly order and physical page numbering remain independent; duplicate entries are not deduplicated and identifiers are not registered or made unique.

`ExtractionDocument.RawRepresentation` exposes an optional borrowed native document result, including when there are zero sections and no local payload. It is excluded from default System.Text.Json serialization. Standard `AIContent.RawRepresentation` and source-reference `GetRawRepresentation()` remain available for payload and region observations. These objects are not implicitly disposed or deep-cloned. The shared contract does not guarantee original HTTP bytes, detachment, response-content status or durable serialization. Producers remain responsible for their owned parser, response, stream and temporary-resource cleanup, including on failure or cancellation; caller streams and borrowed native objects remain borrowed.

Native representations, source references and metadata do not enter text or local payloads automatically. There is no automatic logging, embedding, persistence or document wire serialization contract. Consumers must deliberately select which supporting data to inspect or persist and protect sensitive native data. Null structural content is distinct from an explicitly empty `TextContent`; neither fabricates a text payload for a structural container.

## Payload ownership and memory

Wrapping an existing string in `TextContent` reuses that string, but allocates a payload object. `DataContent` retains supplied byte memory rather than detaching it. Producers must copy provider-owned/recycled buffers before publishing; stable application-owned buffers can be reused. Payloads, their metadata and their source references are not deep-cloned by this model.

Payloads remain mutable. Replacing `Content`, changing `TextContent.Text` or editing collections does not notify the document. A consumer that retains an independently serialized snapshot must refresh it explicitly after mutation.

`ExtractionDocumentTextElement` requires nonnull `TextContent`, including subsequent assignment through the inherited `Content` property or a base reference. Invalid assignments fail before replacing the existing payload. Other elements and custom subclasses retain flexible nullable `AIContent`. The document does not freeze or dispose its borrowed payload objects.

The memory improvement comes from not storing repeated descendant aggregates. Explicitly serializing `DataContent` can create and retain a base64 data URI alongside its original bytes. Text-only consumers should deliberately select semantic text or reject media; they must not obtain embeddings by converting binary payloads to strings.

Generalized annotations/confidence, opaque evidence/page-reference wrappers, per-element metadata/artifact bags, a generic extension envelope, first-class list/item/nesting semantics and normalized barcode/language/link/email/address schemas are outside this first-pass surface. Provider-specific information remains available through native inspection; the package does not perform recognition, infer grounding or define provider-specific feature flags.

The API is subject to change during preview and API review. It is not a replacement for the existing `Microsoft.Extensions.DataIngestion` APIs in this slice.

The package's public API is marked experimental with diagnostic `EXTEXP0019`. Consuming applications can suppress that diagnostic explicitly while evaluating the package; it does not provide compatibility or serialization guarantees.

## Example

```csharp
using Microsoft.Extensions.Documents;
using Microsoft.Extensions.AI;

ExtractionDocument document = new("invoice-42");
ExtractionDocumentSection section = new();
section.Elements.Add(new ExtractionDocumentTextElement(new TextContent("# Invoice"), role: "heading") { HeadingLevel = 1 });
section.Elements.Add(new ExtractionDocumentTextElement(new TextContent("Total: $10")));
ExtractionDocumentTableElement table = new(content: null, rowCount: 1, columnCount: 2);
table.Cells[0, 0] = new ExtractionDocumentTableCell(new ExtractionDocumentTextElement(new TextContent("Item")), role: "rowHeader");
table.Cells[0, 1] = new ExtractionDocumentTableCell(new ExtractionDocumentTextElement(new TextContent("Widget")));
section.Elements.Add(table);
document.Sections.Add(section);

Console.WriteLine(document.GetText());
```

## Supported Platforms

This package includes .NET Framework compatible targets, but is not officially supported on .NET Framework.
