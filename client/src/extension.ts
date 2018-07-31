/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { RazorLanguage } from './RazorLanguage';
import { resolveRazorLanguageServerOptions } from './RazorLanguageServerOptionsResolver';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorProjectTracker } from './RazorProjectTracker';
import { RazorDocumentTracker } from './RazorDocumentTracker';

export async function activate(context: ExtensionContext) {
    let languageServerOptions = resolveRazorLanguageServerOptions();
    let languageServerClient = new RazorLanguageServerClient(languageServerOptions);
    let languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
    let csharpFeature = new RazorCSharpFeature();
    let htmlFeature = new RazorHtmlFeature();
    let projectTracker = new RazorProjectTracker(languageServiceClient);
    let documentTracker = new RazorDocumentTracker(languageServiceClient);
    let localRegistrations: vscode.Disposable[] = [];

    let onStartRegistration = languageServerClient.onStart(() => {
        localRegistrations.push(vscode.languages.registerCompletionItemProvider(RazorLanguage.id, new RazorCompletionItemProvider(csharpFeature, htmlFeature)));
        localRegistrations.push(projectTracker.register());
        localRegistrations.push(documentTracker.register());
        localRegistrations.push(csharpFeature.register());
        localRegistrations.push(htmlFeature.register());

        localRegistrations.push(vscode.workspace.onDidChangeTextDocument((args: vscode.TextDocumentChangeEvent) => {
            if (vscode.window.activeTextEditor && args.document === vscode.window.activeTextEditor.document) {
                csharpFeature.updateDocument(args.document.uri);
                htmlFeature.updateDocument(args.document.uri);
            }
        }));
    });

    let onStopRegistration = languageServerClient.onStop(() => {
        for (let i = 0; i < localRegistrations.length; i++) {
            localRegistrations[i].dispose();
        }
        localRegistrations.length = 0;
    });

    await languageServerClient.start();
    await projectTracker.initialize();
    await documentTracker.initialize();
    await csharpFeature.initialize();
    await htmlFeature.initialize();

    context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration);
}