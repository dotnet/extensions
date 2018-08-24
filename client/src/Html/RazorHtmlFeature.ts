/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServiceClient } from '../RazorLanguageServiceClient';
import { HtmlPreviewDocumentContentProvider } from './HtmlPreviewDocumentContentProvider';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';
import { HtmlTagCompletionProvider } from './HtmlTagCompletionProvider';

export class RazorHtmlFeature {
    public readonly projectionProvider = new HtmlProjectedDocumentContentProvider();
    public readonly previewProvider = new HtmlPreviewDocumentContentProvider(this.projectionProvider);
    private readonly htmlTagCompletionProvider: HtmlTagCompletionProvider;

    constructor(serviceClient: RazorLanguageServiceClient) {
        this.htmlTagCompletionProvider = new HtmlTagCompletionProvider(serviceClient);
    }

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
            this.htmlTagCompletionProvider.register(),
        ];

        return vscode.Disposable.from(...registrations);
    }
}
