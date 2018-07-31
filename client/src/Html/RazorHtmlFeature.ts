/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { HtmlProjectedDocumentContentProvider } from './HtmlProjectedDocumentContentProvider';
import { HtmlPreviewDocumentContentProvider } from './HtmlPreviewDocumentContentProvider';
import { HtmlProjectedDocument } from './HtmlProjectedDocument';

export class RazorHtmlFeature {
    private _projectionProvider: HtmlProjectedDocumentContentProvider;
    private _previewProvider: HtmlPreviewDocumentContentProvider;

    constructor() {

        this._projectionProvider = new HtmlProjectedDocumentContentProvider();
        this._previewProvider = new HtmlPreviewDocumentContentProvider(this._projectionProvider);
    }
    
    public get ProjectionProvider() : HtmlProjectedDocumentContentProvider {
        return this._projectionProvider;
    }
    
    public get PreviewProvider() : HtmlPreviewDocumentContentProvider {
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

        registrations.push(vscode.workspace.registerTextDocumentContentProvider(HtmlProjectedDocument.scheme, this._projectionProvider));
        registrations.push(vscode.workspace.registerTextDocumentContentProvider(HtmlPreviewDocumentContentProvider.scheme, this._previewProvider));
        registrations.push(vscode.commands.registerCommand('extension.showRazorHtmlWindow', () => this._previewProvider.showRazorHtmlWindow()));

        let disposable = vscode.Disposable.from(...registrations);
        return disposable;
    }
}