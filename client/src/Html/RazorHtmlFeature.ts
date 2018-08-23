/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlPreviewDocumentContentProvider } from './HtmlPreviewDocumentContentProvider';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class RazorHtmlFeature {
    public readonly projectionProvider = new HtmlProjectedDocumentContentProvider();
    public readonly previewProvider = new HtmlPreviewDocumentContentProvider(this.projectionProvider);

    public async initialize() {
        const activeProjectedDocument = await this.projectionProvider.getActiveDocument();

        if (activeProjectedDocument) {
            this.updateDocument(activeProjectedDocument.hostDocumentUri);
        }
    }

    public async updateDocument(documentUri: vscode.Uri) {
        await this.projectionProvider.update(documentUri);
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                HtmlProjectedDocumentContentProvider.scheme, this.projectionProvider),
            vscode.workspace.registerTextDocumentContentProvider(
                HtmlPreviewDocumentContentProvider.scheme, this.previewProvider),
            vscode.commands.registerCommand(
                'extension.showRazorHtmlWindow', () => this.previewProvider.showRazorHtmlWindow()),
        ];

        return vscode.Disposable.from(...registrations);
    }
}
