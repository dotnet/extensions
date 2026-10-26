# Microsoft.Extensions.DocumentExtraction.Abstractions

This experimental package defines the extraction-side contract over
`Microsoft.Extensions.Documents.Abstractions`. Providers stream page envelopes
containing semantic observations; `DocumentExtractionDocumentBuilder` explicitly
finalizes those observations into the existing `Documents.Document` model.
It does not define a second canonical tree.

Finalization sorts out-of-order pages and observations, rejects duplicate page
numbers and provider observation identifiers, validates table-cell ownership,
and preserves equal text and duplicate ordered page references. Opaque evidence
references and document-owned artifacts are associated with the resulting
`Documents` elements. The builder owns artifacts until successful finalization;
the finalized result owns them afterward and disposes them with its document.
Cleanup attempts every eligible artifact even when a stream throws. A failed
`Build()` preserves its original error; if cleanup also fails, an
`AggregateException` contains the original error first and the cleanup failures.
Builder disposal reports cleanup failures in an `AggregateException`. Artifacts
already owned by another document remain that document's responsibility.

Unknown, list, barcode, and provider-specific observations can preserve opaque
structured values through `DocumentExtractionExtension.Value` and shared
`DocumentAnnotation` instances, without flattening them into Markdown. This is
transport, not first-class list/item/nesting or normalized detection schemas;
those remain separate review work. The package has no provider, AI, Azure, or
MEDI dependencies and does not fabricate grounding or recognize detections.

`IDocumentExtractionClient.ExtractPagesAsync(Stream, identifier, mediaType,
DocumentExtractionOptions?, CancellationToken)` is the single provider operation. The `ExtractAsync`
extension consumes that same page sequence and returns a disposable result.
The caller owns the input stream; dispose the result to release its document
and registered artifact streams. Page enumeration does not guarantee incremental
network extraction: a provider may finish a remote operation before yielding pages.

## Layout and finalization

Source-page frames retain the reported page number, dimensions, coordinate unit
and origin separately from the assembled page order. Annotations on pages,
observations and table cells carry exact ordered polygons, optional reported
confidence, roles and structured values into the shared document. Unknown units
remain unknown; scores are not normalized or treated as cross-provider calibrated.
Table cells preserve positive row/column spans, producer roles, empty anchors
and annotations independently of content. Blank/image-only pages remain sections
without invented Markdown. Stored document Markdown is assembled only when every
page supplied it; semantic projection remains separate.

## Per-call options and break-glass output

`DocumentExtractionOptions` provides an optional model override and an
adapter-specific `AdditionalProperties` dictionary. An absent override uses
documented provider defaults. Providers must reject unsupported explicit settings,
create fresh requests per operation and avoid mutating shared defaults. Typed
provider feature settings belong to adapters, not Documents. `ExtractAsync`
clones the options and dictionary entries before forwarding; opaque values remain
borrowed and callers must not mutate them while the operation is in flight.
Direct `ExtractPagesAsync` callers must follow the provider's snapshot contract.

`CaptureRawContent` defaults to `false`, including when options are absent.
It controls only retention of available original detached response content in a
per-operation `DocumentSourceOutput`, never a mutable client "last response".
Supplied native SDK results and native annotation representations remain available
by identity independently of this option; native inspection never requires body
capture. Native objects are borrowed and are never implicitly disposed.
Disabling content capture prevents the separate original-content artifact; it is
not redaction and does not strip sensitive or content-bearing fields from retained
native SDK objects, which may themselves contain response data.
Set the flag to `true` to opt into original body retention. Unavailable native/body
data is explicitly reported, not manufactured by serializing mapped objects.
Only content availability may be `Disabled`; native availability remains
`Retained` or `Unavailable`. Providers must distinguish unsupported explicit
settings (which they reject) from legitimately unavailable source output (which
they report honestly even when capture is requested). Finalization rejects retained
original content when capture is off, including nested source associations, and
releases new unowned artifacts instead of silently returning retained data.

Multiple pages may reference one operation. Finalization registers its raw-content
artifact once under document ownership; semantic observation artifact duplication
is still rejected. Disposing the result disposes owned streams, never borrowed
native results/values or artifacts already owned by another document. Raw output
is accessed explicitly through `GetRawRepresentation()`/`GetRawContent()` and
does not automatically enter Markdown, logs, embeddings or ingestion records.
Providers must not capture request credentials or authorization headers.

All public APIs are experimental under `MEDE0001`. The shared Documents APIs
use the separate `EXTEXP0019` diagnostic. Both surfaces are provisional pending
API review. The package deliberately excludes the old extraction-owned tree,
usage/progress APIs, configuration/OTel middleware, URI fetching and AI content
overloads. Restoring information fidelity does not restore those operational APIs.
