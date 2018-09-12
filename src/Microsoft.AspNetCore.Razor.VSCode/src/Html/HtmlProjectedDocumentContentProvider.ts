/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorDocumentChangeEvent } from '../IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from '../RazorDocumentChangeKind';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { getUriPath } from '../UriPaths';

export class HtmlProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = 'razor-html';

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly documentManager: RazorDocumentManager) {
        documentManager.onChange((event) => this.documentChanged(event));
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public provideTextDocumentContent(uri: vscode.Uri) {
        const razorDocument = this.findRazorDocument(uri);
        if (!razorDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t set.');
            return;
        }

        const content = razorDocument.htmlDocument.getContent();
        return content;
    }

    private documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.htmlChanged ||
            event.kind === RazorDocumentChangeKind.opened) {
            this.onDidChangeEmitter.fire(event.document.htmlDocument.uri);
        }
    }

    private findRazorDocument(uri: vscode.Uri) {
        const projectedPath = getUriPath(uri);

        return this.documentManager.documents.find(razorDocument =>
            razorDocument.htmlDocument.path.localeCompare(
                projectedPath, undefined, { sensitivity: 'base' }) === 0);
    }
}
