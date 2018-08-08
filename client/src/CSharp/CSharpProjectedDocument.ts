/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

const cshtmlExtension = '.cshtml';

export class CSharpProjectedDocument {
    public static readonly scheme = 'razor-csharp';

    public static create(hostDocumentUri: vscode.Uri) {
        const extensionlessPath = hostDocumentUri.path.substring(
            0, hostDocumentUri.path.length - cshtmlExtension.length);
        const transformedPath =  `${extensionlessPath}.cs`;
        const projectedUri = vscode.Uri.parse(`${this.scheme}://${transformedPath}`);

        return new CSharpProjectedDocument(projectedUri, hostDocumentUri);
    }

    private constructor(readonly projectedUri: vscode.Uri, readonly hostDocumentUri: vscode.Uri) {
    }
}
