/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class RazorLanguage {
    public static id: string = "razorcore";
    public static documentSelector: vscode.DocumentSelector =  [ { pattern: '**/*.razor' } ];
    public static languageConfig: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration('razor');
    public static serverConfig: vscode.WorkspaceConfiguration = vscode.workspace.getConfiguration('razor.languageServer');
}