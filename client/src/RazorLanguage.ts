/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { DocumentSelector } from 'vscode-languageclient/lib/main';

export class RazorLanguage {
    public static id = 'razor';
    public static documentSelector: DocumentSelector =  [ { pattern: '**/*.cshtml' } ];
    public static languageConfig = vscode.workspace.getConfiguration('razor');
    public static serverConfig = vscode.workspace.getConfiguration('razor.languageServer');
}
