/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocumentContentProvider } from './CSharpProjectedDocumentContentProvider';
import { CSharpProjectedDocument } from './CSharpProjectedDocument';
import { CSharpPreviewDocumentContentProvider } from './CSharpPreviewDocumentContentProvider';

export class RazorCSharpFeature {
    private _projectionProvider: CSharpProjectedDocumentContentProvider;
    private _previewProvider: CSharpPreviewDocumentContentProvider;

    constructor() {

        this._projectionProvider = new CSharpProjectedDocumentContentProvider();
        this._previewProvider = new CSharpPreviewDocumentContentProvider(this._projectionProvider);
    }
    
    public get ProjectionProvider() : CSharpProjectedDocumentContentProvider {
        return this._projectionProvider;
    }
    
    public get PreviewProvider() : CSharpPreviewDocumentContentProvider {
        return this._previewProvider;
    }

    public async initialize(): Promise<void> {
        let activeProjectedDocument = await this._projectionProvider.getActiveDocument();
        
        this.updateDocument(activeProjectedDocument.hostDocumentUri);
    }

    public updateDocument(documentUri: vscode.Uri): void {
        this._projectionProvider.update(documentUri);
        this._previewProvider.update();
    }

    public register(): vscode.Disposable {
        let registrations: vscode.Disposable[] = [];

        registrations.push(vscode.workspace.registerTextDocumentContentProvider(CSharpProjectedDocument.scheme, this._projectionProvider));
        registrations.push(vscode.workspace.registerTextDocumentContentProvider(CSharpPreviewDocumentContentProvider.scheme, this._previewProvider));
        registrations.push(vscode.commands.registerCommand('extension.showRazorCSharpWindow', () => this._previewProvider.showRazorCSharpWindow()));

        let disposable = vscode.Disposable.from(...registrations);
        return disposable;
    }
}