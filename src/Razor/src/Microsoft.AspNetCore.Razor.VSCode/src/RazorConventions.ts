/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { getUriPath } from './UriPaths';

export const virtualHtmlSuffix = '__virtual.html';
export const virtualCSharpSuffix = '__virtual.cs';
export const backgroundVirtualCSharpSuffix = `__bg${virtualCSharpSuffix}`;

export function isRazorCSharpFile(uri: vscode.Uri) {
    const path = getUriPath(uri);
    return path.endsWith(virtualCSharpSuffix);
}

export function isRazorHtmlFile(uri: vscode.Uri) {
    const path = getUriPath(uri);
    return path.endsWith(virtualHtmlSuffix);
}

export function getRazorDocumentUri(uri: vscode.Uri) {
    const path = getUriPath(uri);
    let originalDocumentPath = path.replace(backgroundVirtualCSharpSuffix, '');
    originalDocumentPath = originalDocumentPath.replace(virtualCSharpSuffix, '');
    originalDocumentPath = originalDocumentPath.replace(virtualHtmlSuffix, '');

    const documentUri = vscode.Uri.file(originalDocumentPath);
    return documentUri;
}
