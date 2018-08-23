/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { LanguageKind } from './RPC/LanguageKind';

export interface ProjectionResult {
    uri: vscode.Uri;
    position: vscode.Position;
    languageKind: LanguageKind;
}
