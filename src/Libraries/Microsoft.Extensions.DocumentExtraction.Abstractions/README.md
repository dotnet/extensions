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

Unknown, list, barcode, and provider-specific observations use a typed
`DocumentExtensionElement` with stored markdown/text only. The package retains
no provider payloads, credentials, or fabricated grounding and has no provider,
AI, Azure, or MEDI dependencies.

`IDocumentExtractionClient.ExtractPagesAsync(Stream, identifier, mediaType,
CancellationToken)` is the single provider operation. The `ExtractAsync`
extension consumes that same page sequence and returns a disposable result.
The caller owns the input stream; dispose the result to release its document
and registered artifact streams. Page enumeration does not guarantee incremental
network extraction: a provider may finish a remote operation before yielding pages.

All public APIs are experimental under `MEDE0001`. The shared Documents APIs
use the separate `EXTEXP0019` diagnostic. Both surfaces are provisional pending
API review. This recovery uses Extraction commit `2d28dfd57712b55f941fbb1dbf41311eeaee8c8b`
over the unchanged production model from Documents commit
`d35d5cb74695c4ba970d6ad198006b84354badc0`, forward-ported in prerequisite
PR #7835 (`ff1b5ac49b655209383dea3054e597b9e6387f4f`).

The earlier extraction-owned semantic tree, geometry, options, usage, metadata,
URI fetching, and AI content overloads are not part of this selected contract.
