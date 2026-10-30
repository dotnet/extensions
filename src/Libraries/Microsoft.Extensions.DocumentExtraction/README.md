# Microsoft.Extensions.DocumentExtraction

These experimental packages provide a small extraction capability and optional
composition middleware over the standalone shared Documents exchange model.

`Microsoft.Extensions.DocumentExtraction.Abstractions` defines
`IDocumentExtractionClient.ExtractAsync(Stream, identifier, mediaType, options,
cancellationToken)`, returning `Task<Microsoft.Extensions.Documents.Document>`
directly. There is no required public page/observation staging model, finalization
builder or result wrapper. Adapters build the canonical document and own cleanup
of partial results on failure or cancellation; the caller owns the successful
document and its registered original-response streams.

`Microsoft.Extensions.DocumentExtraction` supplies the optional
`DocumentExtractionClientBuilder`, dependency-injection registration and lifecycle
logging. Delegation snapshots per-call options without mutating arguments or
shared defaults. Logging records only invocation, completion, cancellation and
failure; it does not serialize documents, source references, roles, options,
exceptions, credentials or provider payloads. The returned document and original
failure/cancellation are forwarded unchanged.

Documents works without Extraction. Extraction depends on Documents, not MEDI.
MEDI and other consumers can accept the same shared document from any producer
without requiring an Extraction client. Composition is optional: these libraries
can be used independently and work together through the exchanged document.

Provider mappings preserve nullable/custom text roles and heading levels, images,
table spans and cell roles, typed source geometry and blank-page registration.
Exact supplied `Markdown` remains separate from `ToMarkdown()`, the projection of
the current structure. Native source objects preserve specialized SDK information
without adding generic element metadata, arbitrary artifact bags or normalized
provider-specific schemas.

`CaptureRawContent` defaults to `false`, including absent options. Native SDK
objects remain inspectable independently and are borrowed, never implicitly
disposed. Disabling capture is not redaction: native objects may contain response
data even without a separate original-content artifact. Adapters enforce the body
policy on every source path and transfer available opted-in original body ownership
to the successful document. Capture does not manufacture missing bytes by
serializing mapped objects. Unsupported explicit settings must be rejected,
separately from legitimately unavailable native/body output.

The packages do not include provider clients, provider SDK dependencies, AI
dependencies, MEDI integration, source fetching or package publication.
Production targets are net10.0, net9.0, net8.0, net462 and netstandard2.0.
Public APIs use experimental diagnostic `MEDE0001`; Documents uses `EXTEXP0019`.

PR #7588 temporarily includes the Documents prerequisite from PR #7835.
After that prerequisite merges, reconcile with main while preserving this PR's
history and incorporating reviewed Documents changes. Local candidates are
provisional, not API-approved or published packages. In-memory validation is not
live-provider, full-application or MEDI end-to-end acceptance.
