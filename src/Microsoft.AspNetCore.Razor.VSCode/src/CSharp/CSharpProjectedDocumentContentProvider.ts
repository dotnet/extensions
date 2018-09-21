/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorDocumentChangeEvent } from '../IRazorDocumentChangeEvent';
import { RazorDocumentChangeKind } from '../RazorDocumentChangeKind';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { getUriPath } from '../UriPaths';

export class CSharpProjectedDocumentContentProvider implements vscode.TextDocumentContentProvider {
    public static readonly scheme = 'virtualCSharp-razor';

    private readonly onDidChangeEmitter = new vscode.EventEmitter<vscode.Uri>();

    constructor(private readonly documentManager: RazorDocumentManager) {
        documentManager.onChange((event) => this.documentChanged(event));
    }

    public get onDidChange() { return this.onDidChangeEmitter.event; }

    public async provideTextDocumentContent(uri: vscode.Uri) {
        const razorDocument = this.findRazorDocument(uri);
        if (!razorDocument) {
            vscode.window.showErrorMessage('For some reason the projected document isn\'t available.');
            return;
        }

        const content = razorDocument.csharpDocument.getContent();

        return content;
    }

    public ensureDocumentContent(uri: vscode.Uri) {
        this.onDidChangeEmitter.fire(uri);
    }

    private documentChanged(event: IRazorDocumentChangeEvent) {
        if (event.kind === RazorDocumentChangeKind.csharpChanged ||
            event.kind === RazorDocumentChangeKind.opened) {
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
