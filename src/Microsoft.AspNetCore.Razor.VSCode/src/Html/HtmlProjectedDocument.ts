/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IProjectedDocument } from '../IProjectedDocument';
import { getUriPath } from '../UriPaths';

export class HtmlProjectedDocument implements IProjectedDocument {
    public readonly path: string;
    private content = '';
    private hostDocumentVersion: number | null = null;
    private projectedDocumentVersion = 0;

    public constructor(public readonly uri: vscode.Uri) {
        this.path = getUriPath(uri);
    }

    public get hostDocumentSyncVersion(): number | null {
        return this.hostDocumentVersion;
    }

    public get projectedDocumentSyncVersion(): number {
        return this.projectedDocumentVersion;
    }

    public getContent() {
        return this.content;
    }

    public setContent(content: string, hostDocumentVersion: number | null) {
        this.projectedDocumentVersion++;
        this.hostDocumentVersion = hostDocumentVersion;
        this.content = content;
    }
}
