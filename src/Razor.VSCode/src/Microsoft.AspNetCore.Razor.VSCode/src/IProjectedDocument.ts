/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export interface IProjectedDocument {
    readonly path: string;
    readonly uri: vscode.Uri;
    readonly hostDocumentSyncVersion: number | null;
    readonly projectedDocumentSyncVersion: number;
    getContent(): string;
}
