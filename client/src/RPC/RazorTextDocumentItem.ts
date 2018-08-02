/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class RazorTextDocumentItem {
    constructor (document: vscode.TextDocument) {
        this.languageId = document.languageId;
        this.version = document.version;
        this.text = document.getText();
        if (document.uri.fsPath) {
            this.uri = document.uri.fsPath;
        }
        else {
            this.uri = document.uri.path;
        }
    }

    public readonly languageId: string;
    public readonly version: number;
    public readonly text: string;
    public readonly uri: string;
}