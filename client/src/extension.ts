/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { resolveRazorLanguageServerOptions } from './RazorLanguageServerOptionsResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorProjectTracker } from './RazorProjectTracker';

export async function activate(context: ExtensionContext) {
    const languageServerOptions = resolveRazorLanguageServerOptions();
    const languageServerClient = new RazorLanguageServerClient(languageServerOptions);
    const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
    const csharpFeature = new RazorCSharpFeature();
    const htmlFeature = new RazorHtmlFeature();
    const projectTracker = new RazorProjectTracker(languageServiceClient);
    const localRegistrations: vscode.Disposable[] = [];

    const onStartRegistration = languageServerClient.onStart(() => {
        localRegistrations.push(
            vscode.languages.registerCompletionItemProvider(
                RazorLanguage.id,
                new RazorCompletionItemProvider(csharpFeature, htmlFeature, languageServiceClient)),
            projectTracker.register(),
            csharpFeature.register(),
            htmlFeature.register(),
            vscode.workspace.onDidChangeTextDocument(args => {
                const activeTextEditor = vscode.window.activeTextEditor;
                if (activeTextEditor && activeTextEditor.document === args.document) {
                    csharpFeature.updateDocument(args.document.uri);
                    htmlFeature.updateDocument(args.document.uri);
                }
            }));
    });

    const onStopRegistration = languageServerClient.onStop(() => {
        localRegistrations.forEach(r => r.dispose());
        localRegistrations.length = 0;
    });

    await languageServerClient.start();
    await projectTracker.initialize();
    await csharpFeature.initialize();
    await htmlFeature.initialize();

    context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration);
}
