/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export interface IRazorCodeActionTranslator {
    applyEdit(uri: vscode.Uri, edit: vscode.TextEdit): [vscode.Uri?, vscode.TextEdit?];
    canHandleCodeAction(
        codeAction: vscode.Command,
        codeContext: vscode.CodeActionContext,
        document: vscode.TextDocument): boolean;
    canHandleEdit(uri: vscode.Uri, edit: vscode.TextEdit): boolean;
}
