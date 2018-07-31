/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class LanguageQueryRequest {
    constructor (hostDocumentUri: vscode.Uri, projectedCSharpDocumentUri: vscode.Uri, position: vscode.Position) {
        this.hostDocumentUri = hostDocumentUri.path;
        this.projectedCSharpDocumentUri = projectedCSharpDocumentUri.path;
        this.hostDocumentPosition = position;
    }

    public readonly hostDocumentUri:  string;
    public readonly projectedCSharpDocumentUri:  string;
    public readonly hostDocumentPosition: vscode.Position;
}