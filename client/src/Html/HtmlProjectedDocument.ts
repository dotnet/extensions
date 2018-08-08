/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

const cshtmlExtension = '.cshtml';

export class HtmlProjectedDocument {
    public static readonly scheme = 'razor-html';

    public static create(hostDocumentUri: vscode.Uri) {
        const extensionlessPath = hostDocumentUri.path.substring(
            0, hostDocumentUri.path.length - cshtmlExtension.length);
        const transformedPath = `${extensionlessPath}.html`;
        const projectedUri = vscode.Uri.parse(`${this.scheme}://${transformedPath}`);

        return new HtmlProjectedDocument(projectedUri, hostDocumentUri);
    }
    private constructor(readonly projectedUri: vscode.Uri, readonly hostDocumentUri: vscode.Uri) {
    }
}
