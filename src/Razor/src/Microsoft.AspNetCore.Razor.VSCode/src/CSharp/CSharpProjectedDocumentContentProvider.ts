/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IEventEmitterFactory } from '../IEventEmitterFactory';
import { IRazorDocumentChangeEvent } from '../IRazorDocumentChangeEvent';
import { IRazorDocumentManager } from '../IRazorDocumentManager';
import { RazorDocumentChangeKind } from '../RazorDocumentChangeKind';
import { RazorLogger } from '../RazorLogger';
import { getUriPath } from '../UriPaths';
import * as vscode from '../vscodeAdapter';

export class CSharpProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = 'virtualCSharp-razor';

    private readonly onDidChangeEmitter: vscode.EventEmitter<vscode.Uri>;

    constructor(
        private readonly documentManager: IRazorDocumentManager,
        eventEmitterFactory: IEventEmitterFactory,
        private readonly logger: RazorLogger) {
        documentManager.onChange((event) => this.documentChanged(event));
        this.onDidChangeEmitter = eventEmitterFactory.create<vscode.Uri>();
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async provideTextDocumentContent(uri: vscode.Uri) {
        const razorDocument = this.findRazorDocument(uri);
        if (!razorDocument) {
            // Document was removed from the document manager, meaning there's no more content for this
            // file. Report an empty document.

            if (this.logger.verboseEnabled) {
                this.logger.logVerbose(
                    `Could not find document '${getUriPath(uri)}' when updating the C# buffer. This typically happens when a document is removed.`);
            }
            return '';
        }

        const content = `${razorDocument.csharpDocument.getContent()}
// ${razorDocument.csharpDocument.projectedDocumentSyncVersion}`;

        return content;
    }

    public ensureDocumentContent(uri: vscode.Uri) {
        this.onDidChangeEmitter.fire(uri);
    }

    private documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.csharpChanged ||
            event.kind === RazorDocumentChangeKind.opened ||
            event.kind === RazorDocumentChangeKind.removed) {
            // We also notify changes on document removal in order to tell VSCode that there's no more
            // C# content for the file.

            this.onDidChangeEmitter.fire(event.document.csharpDocument.uri);
        }
    }

    private findRazorDocument(uri: vscode.Uri) {
        const projectedPath = getUriPath(uri);

        return this.documentManager.documents.find(razorDocument =>
            razorDocument.csharpDocument.path.localeCompare(
                projectedPath, undefined, { sensitivity: 'base' }) === 0);
    }
}
