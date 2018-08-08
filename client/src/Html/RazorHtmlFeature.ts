/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlPreviewDocumentContentProvider } from './HtmlPreviewDocumentContentProvider';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';

export class RazorHtmlFeature {
    public readonly projectionProvider = new HtmlProjectedDocumentContentProvider();
    public readonly previewProvider = new HtmlPreviewDocumentContentProvider(this.projectionProvider);

    public async initialize() {
        const activeProjectedDocument = await this.projectionProvider.getActiveDocument();
        this.updateDocument(activeProjectedDocument.hostDocumentUri);
    }

    public updateDocument(documentUri: vscode.Uri) {
        this.projectionProvider.update(documentUri);
        this.previewProvider.update();
    }

    public register() {
        const registrations: vscode.Disposable[] = [
            vscode.workspace.registerTextDocumentContentProvider(
                HtmlProjectedDocument.scheme, this.projectionProvider),
            vscode.workspace.registerTextDocumentContentProvider(
                HtmlPreviewDocumentContentProvider.scheme, this.previewProvider),
            vscode.commands.registerCommand(
                'extension.showRazorHtmlWindow', () => this.previewProvider.showRazorHtmlWindow()),
        ];

        return vscode.Disposable.from(...registrations);
    }
}
