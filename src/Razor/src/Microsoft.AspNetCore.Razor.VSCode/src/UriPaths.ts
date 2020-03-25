/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export function getUriPath(uri: vscode.Uri) {
    let newUri = uri;
    if (uri.authority && uri.scheme === 'file') {
        // UNC path. The host(authority) part of this is case-insensitive. Let's normalize it so we don't create duplicate documents.
        const authority = uri.authority.toLowerCase();
        newUri = uri.with({ authority });
    }

    return newUri.fsPath || newUri.path;
}
