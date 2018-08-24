/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { ProvisionalCompletionOrchestrator } from './ProvisionalCompletionOrchestrator';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorDocumentTracker } from './RazorDocumentTracker';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageConfiguration } from './RazorLanguageConfiguration';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { resolveRazorLanguageServerOptions } from './RazorLanguageServerOptionsResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorProjectTracker } from './RazorProjectTracker';

let activationResolver: (value?: any) => void;
export const extensionActivated = new Promise(resolve => {
    activationResolver = resolve;
});

export async function activate(context: ExtensionContext) {
    const languageServerOptions = resolveRazorLanguageServerOptions();
    const languageConfiguration = new RazorLanguageConfiguration();
    const languageServerClient = new RazorLanguageServerClient(languageServerOptions);
    const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
    const csharpFeature = new RazorCSharpFeature(languageServerClient);
    const htmlFeature = new RazorHtmlFeature(languageServiceClient);
    const projectTracker = new RazorProjectTracker(languageServiceClient);
    const documentTracker = new RazorDocumentTracker(languageServiceClient);
    const localRegistrations: vscode.Disposable[] = [];

    const onStartRegistration = languageServerClient.onStart(() => {
        const provisionalCompletionOrchestrator = new ProvisionalCompletionOrchestrator(
            csharpFeature,
            languageServiceClient);
        const completionItemProvider = new RazorCompletionItemProvider(
            csharpFeature,
            htmlFeature,
            languageServiceClient,
            provisionalCompletionOrchestrator);

        localRegistrations.push(
            languageConfiguration.register(),
            provisionalCompletionOrchestrator.register(),
            vscode.languages.registerCompletionItemProvider(
                RazorLanguage.id,
                completionItemProvider,
                '.', '<', '@'),
            projectTracker.register(),
            documentTracker.register(),
            csharpFeature.register(),
            htmlFeature.register(),
            vscode.workspace.onDidChangeTextDocument(async args => {
                const activeTextEditor = vscode.window.activeTextEditor;
                if (activeTextEditor && activeTextEditor.document === args.document) {
                    await htmlFeature.updateDocument(args.document.uri);
                }
            }));
    });

    const onStopRegistration = languageServerClient.onStop(() => {
        localRegistrations.forEach(r => r.dispose());
        localRegistrations.length = 0;
    });

    await languageServerClient.start();
    await projectTracker.initialize();
    await documentTracker.initialize();
    await htmlFeature.initialize();

    context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration);

    activationResolver();
}
