/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IProjectedDocument } from '../IProjectedDocument';
import { ServerTextChange } from '../RPC/ServerTextChange';
import { getUriPath } from '../UriPaths';
import * as vscode from '../vscodeAdapter';

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

    public update(edits: ServerTextChange[], hostDocumentVersion: number) {
        this.hostDocumentVersion = hostDocumentVersion;

        if (edits.length === 0) {
            return;
        }

        let content = this.content;
        for (const edit of edits.reverse()) {
            // TODO: Use a better data structure to represent the content, string concatenation is slow.
            content = this.getEditedContent(edit.newText, edit.span.start, edit.span.start + edit.span.length, content);
        }

        this.setContent(content);
    }

    public getContent() {
        return this.content;
    }

    public reset() {
        this.projectedDocumentVersion++;
        this.hostDocumentVersion = null;
        this.setContent('');
    }

    private getEditedContent(newText: string, start: number, end: number, content: string) {
        const before = content.substr(0, start);
        const after = content.substr(end);
        content = `${before}${newText}${after}`;

        return content;
    }

    private setContent(content: string) {
        this.projectedDocumentVersion++;
        this.content = content;
    }
}
