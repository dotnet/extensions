/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

export class CSharpProjectedDocument {
    public static readonly scheme: string = "razor-csharp";
    
    private _projectedUri : vscode.Uri;
    private _hostDocumentUri : vscode.Uri;

    private constructor (projectedUri: vscode.Uri, hostDocumentUri: vscode.Uri) {
        this._projectedUri = projectedUri;
        this._hostDocumentUri = hostDocumentUri;
    }

    public get projectedUri() : vscode.Uri { return this._projectedUri; }

    public get hostDocumentUri() : vscode.Uri { return this._hostDocumentUri; }

    public static create(hostDocumentUri: vscode.Uri): CSharpProjectedDocument {
        let extensionlessPath = hostDocumentUri.path.substring(0, hostDocumentUri.path.length - 6);
        let transformedPath =  extensionlessPath + ".cs";
        let projectedUri = vscode.Uri.parse(this.scheme + "://" + transformedPath);

        return new CSharpProjectedDocument(projectedUri, hostDocumentUri);
    }
}