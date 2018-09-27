/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { CSharpProjectedDocument } from './CSharp/CSharpProjectedDocument';
import { CSharpProjectedDocumentContentProvider } from './CSharp/CSharpProjectedDocumentContentProvider';
import { HtmlProjectedDocument } from './Html/HtmlProjectedDocument';
import { HtmlProjectedDocumentContentProvider } from './Html/HtmlProjectedDocumentContentProvider';
import { IRazorDocument } from './IRazorDocument';
import { getUriPath } from './UriPaths';

export function createDocument(uri: vscode.Uri) {
    const csharpDocument = createProjectedCSharpDocument(uri);
    const htmlDocument = createProjectedHtmlDocument(uri);
    const path = getUriPath(uri);

    const document: IRazorDocument = {
        uri,
        path,
        csharpDocument,
        htmlDocument,
    };

    return document;
}

function createProjectedHtmlDocument(hostDocumentUri: vscode.Uri) {
    // Index.cshtml => __Index.cshtml.html
    const projectedPath = `__${hostDocumentUri.path}.html`;
    const projectedUri = vscode.Uri.parse(`${HtmlProjectedDocumentContentProvider.scheme}://${projectedPath}`);
    const projectedDocument = new HtmlProjectedDocument(projectedUri);

    return projectedDocument;
}

function createProjectedCSharpDocument(hostDocumentUri: vscode.Uri) {
    // Index.cshtml => __Index.cshtml__virtual.cs
    const projectedPath = `__${hostDocumentUri.path}__virtual.cs`;
    const projectedUri = vscode.Uri.parse(`${CSharpProjectedDocumentContentProvider.scheme}://${projectedPath}`);
    const projectedDocument = new CSharpProjectedDocument(projectedUri);

    return projectedDocument;
}
