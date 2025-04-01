import DOMPurify from './lib/dompurify/dist/purify.es.mjs';
import * as marked from './lib/marked/dist/marked.esm.js';

const purify = DOMPurify(window);

customElements.define('assistant-message', class extends HTMLElement {
    static observedAttributes = ['markdown'];

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'markdown') {
            newValue = newValue.replace(/<citation.*?<\/citation>/gs, '');
            const elements = marked.parse(newValue.replace(/</g, '&lt;'));
            this.innerHTML = purify.sanitize(elements, { KEEP_CONTENT: false });

            // Within text nodes, unescape the &lt; entities otherwise it will be displayed
            // to the user as escaped if the element uses preformatted styling. This is safe
            // because we're only updating the text content of text nodes.
            const walker = document.createTreeWalker(this, NodeFilter.SHOW_TEXT);
            while (walker.nextNode()) {
                walker.currentNode.textContent = walker.currentNode.textContent.replace(/&lt;/g, '<');
            }
        }
    }
});
