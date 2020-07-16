/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { TextDocumentIdentifier } from 'vscode-languageclient';

export class SemanticTokensRequest {
    public readonly textDocument: TextDocumentIdentifier;

    constructor(
        razorDocumentUri: vscode.Uri) {
        this.textDocument = TextDocumentIdentifier.create(razorDocumentUri.toString());
    }
}
