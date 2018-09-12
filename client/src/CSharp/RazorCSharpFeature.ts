/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorDocumentManager } from '../RazorDocumentManager';
import { CSharpPreviewDocumentContentProvider } from './CSharpPreviewDocumentContentProvider';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';

export class RazorCSharpFeature {
    public readonly projectionProvider: CSharpProjectedDocumentContentProvider;
    public readonly previewProvider: CSharpPreviewDocumentContentProvider;

    constructor(documentManager: RazorDocumentManager) {
        this.projectionProvider = new CSharpProjectedDocumentContentProvider(documentManager);
        this.previewProvider = new CSharpPreviewDocumentContentProvider(documentManager);
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

        return vscode.Disposable.from(...registrations);
    }
}
