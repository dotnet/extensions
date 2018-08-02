/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class LanguageQueryRequest {
    constructor (position: vscode.Position, uri: vscode.Uri) {
        this.position = position;
        this.uri = uri.fsPath;
    }

    public readonly position: vscode.Position;
    public readonly uri:  string;
}