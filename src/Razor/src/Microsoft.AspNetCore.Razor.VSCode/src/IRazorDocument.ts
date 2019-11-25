/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IProjectedDocument } from './IProjectedDocument';

export interface IRazorDocument {
    readonly path: string;
    readonly uri: vscode.Uri;
    readonly csharpDocument: IProjectedDocument;
    readonly htmlDocument: IProjectedDocument;
}
