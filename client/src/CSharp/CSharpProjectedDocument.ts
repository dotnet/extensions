/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ServerTextChange } from '../RPC/ServerTextChange';

export class CSharpProjectedDocument {
    private content = '';

    public constructor(
        readonly projectedUri: vscode.Uri,
        readonly hostDocumentUri: vscode.Uri,
        readonly onChange: () => void) {
    }

    public applyEdits(edits: ServerTextChange[]) {
        for (const edit of edits) {
            // TODO: Use a better data structure to represent the content, string concats
            // are slow.
            const before = this.content.substr(0, edit.span.start);
            const after = this.content.substr(edit.span.end);
            this.setContent(`${before}${edit.newText}${after}`);
        }
    }

    public getContent() {
        return this.content;
    }

    private setContent(content: string) {
        this.content = content;
        this.onChange();
    }
}
