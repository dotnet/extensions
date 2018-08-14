/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { TextDocumentChangeEvent } from 'vscode';
import { Middleware } from 'vscode-languageclient/lib/main';

export class RazorLanguageServerMiddleware implements Middleware {
    private lastSeenDocumentVersion: number;

    constructor() {
        this.lastSeenDocumentVersion = -1;
    }

    public didChange(data: TextDocumentChangeEvent, next: (data: TextDocumentChangeEvent) => void) {
        if (data.document.version === this.lastSeenDocumentVersion) {
            // We've already fired a change event for this text document version, noop so we don't flood the server
            // with duplicate requests.
            // Working around issue https://github.com/Microsoft/vscode-languageserver-node/issues/392
            return;
        }

        this.lastSeenDocumentVersion = data.document.version;
        next(data);
    }
}
