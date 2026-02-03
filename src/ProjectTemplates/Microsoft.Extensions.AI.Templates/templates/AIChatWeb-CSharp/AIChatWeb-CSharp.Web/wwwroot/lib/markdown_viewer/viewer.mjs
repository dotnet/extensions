import { parse } from '../marked/dist/marked.esm.js';
import purify from '../dompurify/dist/purify.es.mjs';

const url = new URL(window.location);
const fileUrl = url.searchParams.get('file');
if (!fileUrl) {
  throw new Error('File not specified in the URL query string');
}

var response = await fetch(fileUrl);
var text = await response.text();

document.getElementById('content').innerHTML = purify.sanitize(parse(text));
