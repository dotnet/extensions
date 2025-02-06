async function loadPDFViewer() {
  if (!pdfjsLib.getDocument || !pdfjsViewer.PDFViewer) {
    console.error("The PDF.js library is not present on this page");
    return;
  }

  pdfjsLib.GlobalWorkerOptions.workerSrc = "./pdfjs-dist/dist/build/pdf.worker.mjs";

  // Extract the file path from the URL query string.
  const url = new URL(window.location);
  const fileUrl = url.searchParams.get("file");
  if (!fileUrl) {
    console.error("File not specified in the URL query string");
    return;
  }

  const container = document.getElementById("viewerContainer");
  const eventBus = new pdfjsViewer.EventBus();

  // Enable hyperlinks within PDF files.
  const pdfLinkService = new pdfjsViewer.PDFLinkService({
    eventBus,
  });

  // Enable the find controller.
  const pdfFindController = new pdfjsViewer.PDFFindController({
    eventBus,
    linkService: pdfLinkService,
  });

  // Create the PDF viewer.
  const pdfViewer = new pdfjsViewer.PDFViewer({
    container,
    eventBus,
    linkService: pdfLinkService,
    findController: pdfFindController,
  });
  pdfLinkService.setViewer(pdfViewer);

  // Allow navigation to a citaiton from the URL hash.
  eventBus.on("pagesinit", function () {
    pdfLinkService.setHash(window.location.hash.substring(1));
  });

  // Define how the "search" query parameter is handled.
  eventBus.on("findfromurlhash", function(evt) {
    eventBus.dispatch("find", {
      source: evt.source,
      type: "",
      query: evt.query,
      caseSensitive: false,
      entireWord: false,
      highlightAll: false,
      findPrevious: false,
      matchDiacritics: true,
    });
  });

  // Load and initialize the document.
  const pdfDocument = await pdfjsLib.getDocument({
    url: fileUrl,
    enableXfa: true,
  }).promise;

  pdfViewer.setDocument(pdfDocument);
  pdfLinkService.setDocument(pdfDocument, null);
}

await loadPDFViewer();
