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
    readonly tagHelpers: any[];
    readonly configuration: any;
    readonly targetFramework: string;
    readonly lastUpdated: Date;
}
