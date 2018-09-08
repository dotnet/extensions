/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IProjectedDocument } from '../IProjectedDocument';

export class HtmlProjectedDocument implements IProjectedDocument {
    private content = '';
    private hostDocumentVersion: number | null = null;

    public constructor(
        readonly projectedUri: vscode.Uri,
        readonly hostDocumentUri: vscode.Uri,
        readonly onChange: () => void) {
    }

    public get hostDocumentSyncVersion(): number | null {
        return this.hostDocumentVersion;
    }

    public getContent() {
        return this.content;
    }

    public setContent(content: string, hostDocumentVersion: number) {
        this.content = content;
        this.hostDocumentVersion = hostDocumentVersion;
        this.onChange();
    }
}
