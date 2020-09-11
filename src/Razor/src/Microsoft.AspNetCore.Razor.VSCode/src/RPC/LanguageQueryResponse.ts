/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { LanguageKind } from './LanguageKind';

export interface LanguageQueryResponse {
    kind: LanguageKind;
    position: vscode.Position;
    positionIndex: number;
    hostDocumentVersion: number | undefined;
}
