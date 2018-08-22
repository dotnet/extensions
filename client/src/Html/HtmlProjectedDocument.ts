/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class HtmlProjectedDocument {
    private content = '';

    public constructor(
        readonly projectedUri: vscode.Uri,
        readonly hostDocumentUri: vscode.Uri,
        readonly onChange: () => void) {
    }

    public getContent() {
        return this.content;
    }

    public setContent(content: string) {
        this.content = content;
        this.onChange();
    }
}
