/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from './RazorDocumentChangeKind';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';

export class RazorDocumentTracker {
    constructor(
        private readonly razorDocumentManager: RazorDocumentManager,
        private readonly languageServiceClient: RazorLanguageServiceClient) {
    }

    public register() {
        const registration = this.razorDocumentManager.onChange(event => this.onDocumentChange(event));

        return registration;
    }

    private async onDocumentChange(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.added) {
            await this.languageServiceClient.addDocument(event.document.uri);
        } else if (event.kind === RazorDocumentChangeKind.removed) {
            await this.languageServiceClient.removeDocument(event.document.uri);
        }
    }
}
