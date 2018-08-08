/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpPreviewDocumentContentProvider } from './CSharpPreviewDocumentContentProvider';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class RazorCSharpFeature {
    public readonly projectionProvider = new CSharpProjectedDocumentContentProvider();
    public readonly previewProvider = new CSharpPreviewDocumentContentProvider(this.projectionProvider);

    public async initialize() {
        const activeProjectedDocument = await this.projectionProvider.getActiveDocument();
        this.updateDocument(activeProjectedDocument.hostDocumentUri);
    }

    public updateDocument(documentUri: vscode.Uri) {
        this.projectionProvider.update(documentUri);
        this.previewProvider.update();
    }

    public register() {
        const registrations = [
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpProjectedDocument.scheme, this.projectionProvider),
            vscode.workspace.registerTextDocumentContentProvider(
                CSharpPreviewDocumentContentProvider.scheme, this.previewProvider),
            vscode.commands.registerCommand(
                'extension.showRazorCSharpWindow', () => this.previewProvider.showRazorCSharpWindow()),
        ];

        return vscode.Disposable.from(...registrations);
    }
}
