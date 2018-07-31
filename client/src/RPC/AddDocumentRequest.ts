/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorTextDocumentItem } from './RazorTextDocumentItem';

export class AddDocumentRequest {
    constructor(textDocument: vscode.TextDocument) {
        this.textDocument = new RazorTextDocumentItem(textDocument);
    }

    public readonly textDocument: RazorTextDocumentItem;
}