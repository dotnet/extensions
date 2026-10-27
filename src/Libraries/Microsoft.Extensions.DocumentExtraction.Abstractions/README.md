# Microsoft.Extensions.DocumentExtraction.Abstractions

This experimental package defines a small extraction capability over the standalone
`Microsoft.Extensions.Documents.Abstractions` exchange model. It does not define a
second document tree or require an authoring, page-streaming or observation protocol.

The primary operation is:

```csharp
Task<Document> ExtractAsync(
    Stream document,
    string identifier,
    string mediaType,
    DocumentExtractionOptions? options = null,
    CancellationToken cancellationToken = default);
```

An adapter returns the canonical `Microsoft.Extensions.Documents.Document` directly.
The caller owns the input stream and the returned document. Disposing the document
releases its registered original-response body streams, never borrowed native SDK
objects. Adapters own partial-result cleanup on failure or cancellation, must attempt
every acquired body, and must preserve the primary error if cleanup also fails.
They must honor cancellation without disposing the caller's input.

Documents can be constructed or consumed without Extraction. Extraction depends on
Documents, not MEDI, AI or provider SDK packages. MEDI or another consumer can accept
the same shared document from any producer without requiring Extraction. The
packages compose through the exchanged document rather than a mandatory pipeline.

## Mapping and source fidelity

Adapters map provider output to shared text, image, table and section elements.
Text roles such as `heading`, `pageHeader`, `pageFooter`, custom roles and optional
heading levels are preserved without inventing unspecified information.
Exact supplied `Markdown` is distinct from `ToMarkdown()`, the projection of the
current document structure.

Typed `Sources` retain source pages, coordinate frames and ordered polygons, with
explicit native-object access. A source without a polygon identifies a page, not a
precise location. Adapters register real blank-page frames and source operations
even when no element refers to them. Table spans, roles and sources are independent
of whether a cell has content.

The Extraction contract has no staged element metadata, arbitrary semantic-artifact
bags, generalized detections or extension-element schema. Specialized SDK data is
available through borrowed native source objects; document-level metadata remains
available for consumers to populate explicitly. No source association establishes
verified grounding or calibrated confidence.

## Per-call options and break-glass output

`DocumentExtractionOptions` provides an optional model override and an
adapter-specific `AdditionalProperties` dictionary. Providers must reject unsupported
explicit settings, create fresh requests per operation, and avoid mutating caller
arguments or shared defaults. Implementations must snapshot options before
asynchronous work. `DelegatingDocumentExtractionClient` copies scalar values and
dictionary entries before forwarding; opaque values remain borrowed and callers
must not mutate them while an operation is in flight.

`CaptureRawContent` defaults to `false`, including when options are absent. It
controls only a separate original detached response-content artifact. Supplied
native SDK results and source-reference representations remain inspectable by
identity independently of this option and are never implicitly disposed.
Disabling capture is not redaction: retained native objects may themselves contain
sensitive fields or response data.

Adapters must not retain original body artifacts when capture is off, including
through nested source associations. When capture is on, they retain only actual
available response bytes, not serialization of mapped objects. `ContentStatus`
honestly distinguishes `Disabled`, `Unavailable` and `Retained`; a native getter
returning `null` means that representation was not supplied. Unsupported explicit
settings are errors, not the same as legitimately unavailable output.

Successful source-body registration transfers ownership to the returned document.
Shared operation bodies are owned once. Borrowed native objects and bodies already
owned by another document must not be implicitly disposed during failed extraction.
Raw output is accessed explicitly through `GetRawRepresentation()`/`GetRawContent()`
and must not automatically enter Markdown, logs, embeddings or ingestion records.
Providers must not capture request credentials or authorization headers.

All public Extraction APIs are experimental under `MEDE0001`; shared Documents uses
`EXTEXP0019`. The package does not include provider implementations, MEDI adapters,
URI fetching, progress/usage APIs, configuration middleware or OpenTelemetry.
