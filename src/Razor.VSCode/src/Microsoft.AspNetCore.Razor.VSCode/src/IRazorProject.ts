/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { IRazorProjectConfiguration } from './IRazorProjectConfiguration';

export interface IRazorProject {
    readonly uri: vscode.Uri;
    readonly path: string;
    readonly configuration?: IRazorProjectConfiguration;
}
