/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IProjectedDocument } from 'microsoft.aspnetcore.razor.vscode/dist/IProjectedDocument';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';

export class TestProjectedDocument implements IProjectedDocument {
    constructor(
        public readonly content: string,
        public readonly uri: vscode.Uri,
        public readonly path = uri.path) {
    }

    public get hostDocumentSyncVersion(): number | null {
        throw new Error('Not Implemented.');
    }

    public get projectedDocumentSyncVersion(): number {
        throw new Error('Not Implemented.');
    }

    public getContent(): string {
        return this.content;
    }
}
