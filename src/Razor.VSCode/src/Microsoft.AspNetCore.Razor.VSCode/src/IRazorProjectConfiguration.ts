/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export interface IRazorProjectConfiguration {
    readonly path: string;
    readonly uri: vscode.Uri;
    readonly projectPath: string;
    readonly projectUri: vscode.Uri;
    readonly configuration: any;
    readonly rootNamespace: string;
    readonly projectWorkspaceState: any;
    readonly lastUpdated: Date;
    readonly documents: any;
    readonly serializationFormat: string;
}
