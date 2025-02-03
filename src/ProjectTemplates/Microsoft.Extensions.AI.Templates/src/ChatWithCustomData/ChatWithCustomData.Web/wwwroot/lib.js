import DOMPurify from 'dompurify';
import * as marked from 'marked';
const purify = DOMPurify(window);

customElements.define('assistant-message', class extends HTMLElement {
    static observedAttributes = ['markdown'];

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'markdown') {
            const elements = marked.parse(newValue);
            this.innerHTML = purify.sanitize(elements, { KEEP_CONTENT: false });
        }
    }
});
