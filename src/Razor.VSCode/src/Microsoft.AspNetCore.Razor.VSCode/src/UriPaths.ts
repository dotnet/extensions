/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export function getUriPath(uri: vscode.Uri) {
    const uriPath = uri.fsPath || uri.path;
    return uriPath;
}
