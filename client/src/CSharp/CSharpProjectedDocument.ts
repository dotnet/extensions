/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ServerTextChange } from '../RPC/ServerTextChange';

export class CSharpProjectedDocument {
    private content = '';
    private preProvisionalContent: string | undefined;
    private provisionalEditAt: number | undefined;

    public constructor(
        readonly projectedUri: vscode.Uri,
        readonly hostDocumentUri: vscode.Uri,
        readonly onChange: () => void) {
    }

    public applyEdits(edits: ServerTextChange[]) {
        this.removeProvisionalDot();

        if (edits.length === 0) {
            return;
        }

        let content = this.content;
        for (const edit of edits) {
            // TODO: Use a better data structure to represent the content, string concats
            // are slow.
            content = this.getEditedContent(edit.newText, edit.span.start, edit.span.end, content);
        }

        this.setContent(content);
    }

    public getContent() {
        return this.content;
    }

    // A provisional dot represents a '.' that's inserted into the projected document but will be
    // removed prior to any edits that get applied. In Razor's case a provisional dot is used to
    // show completions after an expression for a dot that's usually interpreted as Html.
    public addProvisionalDotAt(index: number) {
        if (this.provisionalEditAt === index) {
            // Edits already applied.
            return;
        }

        this.removeProvisionalDot();

        const newContent = this.getEditedContent('.', index, index, this.content);
        this.preProvisionalContent = this.content;
        this.provisionalEditAt = index;
        this.setContent(newContent);
    }

    public removeProvisionalDot() {
        if (this.provisionalEditAt && this.preProvisionalContent) {
            // Undo provisional edit if one was applied.
            this.setContent(this.preProvisionalContent);
            this.provisionalEditAt = undefined;
            this.preProvisionalContent = undefined;
        }
    }

    private getEditedContent(newText: string, start: number, end: number, content: string) {
        const before = content.substr(0, start);
        const after = content.substr(end);
        content = `${before}${newText}${after}`;

        return content;
    }

    private setContent(content: string) {
        this.content = content;
        this.onChange();
    }
}
