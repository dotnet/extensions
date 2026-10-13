# Microsoft.Extensions.DocumentExtraction

These experimental packages define the extraction-side waist for the neutral
`Microsoft.Extensions.Documents.Abstractions` model.

`Microsoft.Extensions.DocumentExtraction.Abstractions` contains a streaming
`IDocumentExtractionClient`, page and observation contracts, and
`DocumentExtractionDocumentBuilder`. The builder accepts pages in any arrival
order, sorts them and their observations during explicit finalization, rejects
duplicate page numbers and provider observation identifiers, preserves equal
text from different observations, and preserves opaque page/evidence references
without asserting grounding. It maps tables, images, and unknown/list/barcode
extensions into the existing `Documents` element types. `DocumentArtifact`
instances are transferred to the finalized `DocumentExtractionResult` and are
disposed when the result is disposed or finalization fails.

`Microsoft.Extensions.DocumentExtraction` contains the optional
`DocumentExtractionClientBuilder`, dependency-injection registration, delegation,
and lifecycle logging middleware. Logging is intentionally limited to lifecycle,
page-number, and observation-count fields; source content, evidence values,
artifacts, credentials, and raw provider payloads are never serialized or
logged.

The packages do not include provider clients, provider SDKs, AI dependencies,
MEDI/DataIngestion integration, source fetching, or package publication.

The selected baseline is builder/DI/delegation/lifecycle logging, not the old
options-configuration or OpenTelemetry middleware. Public APIs use experimental
diagnostic `MEDE0001`; the shared Documents dependency uses `EXTEXP0019`.
Production targets are net10.0, net9.0, net8.0, net462, and netstandard2.0.

PR #7588 temporarily includes the Documents prerequisite from PR #7835
(`ff1b5ac49b655209383dea3054e597b9e6387f4f`). After that prerequisite merges,
reconcile with main while preserving this PR's history and incorporating any
reviewed Documents API changes. Local development candidates are provisional,
not API-approved or published packages, and do not establish provider acceptance
or MEDI end-to-end compatibility.
