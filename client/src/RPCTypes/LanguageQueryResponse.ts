/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class LanguageQueryResponse {
    constructor (textDocumentUri: vscode.Uri, position: vscode.Position) {
        this.textDocumentUri = textDocumentUri.path;
        this.position = position;
    }

    public readonly textDocumentUri: string;
    public readonly position: vscode.Position;
}