/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { IRazorDocument } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocument';
import { IRazorDocumentChangeEvent } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocumentChangeEvent';
import { IRazorDocumentManager } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocumentManager';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';

export class TestRazorDocumentManager implements IRazorDocumentManager {
    public get onChange(): vscode.Event<IRazorDocumentChangeEvent> {
        throw new Error('Not implemented');
    }

    public get documents(): IRazorDocument[] {
        throw new Error('Not implemented');
    }

    public getDocument(uri: vscode.Uri): Promise<IRazorDocument> {
        throw new Error('Not implemented');
    }

    public getActiveDocument(): Promise<IRazorDocument | null> {
        throw new Error('Not implemented');
    }

    public initialize(): Promise<void> {
        throw new Error('Not implemented');
    }

    public register(): vscode.Disposable {
        throw new Error('Not implemented');
    }
}
