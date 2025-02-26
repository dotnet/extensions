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
        }
    }
});
