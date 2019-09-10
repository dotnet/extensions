/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class LanguageQueryRequest {
    public readonly uri: string;

    constructor(public readonly position: vscode.Position, uri: vscode.Uri) {
        this.uri = uri.toString();
    }
}
