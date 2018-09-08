/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageServerClient } from '../RazorLanguageServerClient';
import { UpdateCSharpBufferRequest } from '../RPC/UpdateCSharpBufferRequest';
import { CSharpPreviewDocumentContentProvider } from './CSharpPreviewDocumentContentProvider';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class RazorCSharpFeature {
    public readonly projectionProvider = new CSharpProjectedDocumentContentProvider();
    public readonly previewProvider = new CSharpPreviewDocumentContentProvider(this.projectionProvider);

    constructor(private serverClient: RazorLanguageServerClient) {
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpProjectedDocumentContentProvider.scheme, this.projectionProvider),
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpPreviewDocumentContentProvider.scheme, this.previewProvider),
            vscode.commands.registerCommand(
                'extension.showRazorCSharpWindow', () => this.previewProvider.showRazorCSharpWindow()),
        ];

        this.serverClient.onRequest(
            'updateCSharpBuffer',
            updateBufferRequest => this.updateCSharpBuffer(updateBufferRequest));

        return vscode.Disposable.from(...registrations);
    }

    private async updateCSharpBuffer(updateBufferRequest: UpdateCSharpBufferRequest) {
        const hostDocumentUri = vscode.Uri.file(updateBufferRequest.hostDocumentFilePath);
        const projectedDocument = await this.projectionProvider.getDocument(hostDocumentUri);
        if (!projectedDocument.hostDocumentSyncVersion ||
            projectedDocument.hostDocumentSyncVersion < updateBufferRequest.hostDocumentVersion) {
            projectedDocument.update(updateBufferRequest.changes, updateBufferRequest.hostDocumentVersion);
        }
    }
}
