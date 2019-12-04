/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class RazorCodeLens extends vscode.CodeLens {
    constructor(
        range: vscode.Range,
        public uri: vscode.Uri,
        public document: vscode.TextDocument,
        command?: vscode.Command) {

        super(range, command);
    }
}
