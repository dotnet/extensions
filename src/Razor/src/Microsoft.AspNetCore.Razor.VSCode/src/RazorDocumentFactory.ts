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
import { virtualCSharpSuffix, virtualHtmlSuffix } from './RazorConventions';
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
    // Index.cshtml => Index.cshtml__virtual.html
    const path = getUriPath(hostDocumentUri);
    const projectedPath = `${path}${virtualHtmlSuffix}`;
    let uri = vscode.Uri.file(projectedPath);
    uri = uri.with({ scheme: HtmlProjectedDocumentContentProvider.scheme });
    const projectedDocument = new HtmlProjectedDocument(uri);

    return projectedDocument;
}

function createProjectedCSharpDocument(hostDocumentUri: vscode.Uri) {
    // Index.cshtml => Index.cshtml__virtual.cs
    const path = getUriPath(hostDocumentUri);
    const projectedPath = `${path}${virtualCSharpSuffix}`;
    let uri = vscode.Uri.file(projectedPath);
    uri = uri.with({ scheme: CSharpProjectedDocumentContentProvider.scheme });
    const projectedDocument = new CSharpProjectedDocument(uri);

    return projectedDocument;
}
