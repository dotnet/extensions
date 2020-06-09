/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguageFeatureBase } from './RazorLanguageFeatureBase';

export class RazorHoverProvider
    extends RazorLanguageFeatureBase
    implements vscode.HoverProvider {

    public async provideHover(
        document: vscode.TextDocument, position: vscode.Position,
        token: vscode.CancellationToken) {

        const projection = await this.getProjection(document, position, token);
        if (!projection) {
            return;
        }

        const results = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            projection.uri,
            projection.position);

        if (!results || results.length === 0) {
            return;
        }

        // At the vscode.HoverProvider layer we can only return a single hover result. Because of this limitation we need to
        // be smart about combining multiple hovers content or only take a single hover result. For now we'll only take one
        // of them and then based on user feedback we can change this approach in the future.
        const applicableHover = results.filter(item => item.range)[0];
        if (!applicableHover) {
            // No hovers available with a range.
            return;
        }

        // Re-map the projected hover range to the host document range
        const remappedResponse = await this.serviceClient.mapToDocumentRanges(
            projection.languageKind,
            [applicableHover.range!],
            document.uri);

        if (!remappedResponse ||
            !remappedResponse.ranges ||
            !remappedResponse.ranges[0]) {
            // Couldn't remap the projected hover location, there's no hover information available.
            return;
        }

        if (document.version !== remappedResponse.hostDocumentVersion) {
            // This hover result is for a different version of the text document, bail.
            return;
        }

        const rewrittenContent = new Array<vscode.MarkedString>();
        for (const content of applicableHover.contents) {
            // For some reason VSCode doesn't respect the hover contents as-is. Because of this we need to look at each permutation
            // of "content" (MarkdownString | string | { language: string; value: string }) and re-compute it as a MarkdownString or
            // string.

            if (typeof content === 'string') {
                rewrittenContent.push(content);
            } else if ((content as { language: string; value: string }).language) {
                const contentObject = (content as { language: string; value: string });
                const markdownString = new vscode.MarkdownString();
                markdownString.appendCodeblock(contentObject.value, contentObject.language);
                rewrittenContent.push(markdownString);
            } else {
                const contentValue = (content as vscode.MarkdownString).value;
                const markdownString = new vscode.MarkdownString(contentValue);
                rewrittenContent.push(markdownString);
            }
        }

        const hover = new vscode.Hover(rewrittenContent, remappedResponse.ranges[0]);
        return hover;
    }
}
