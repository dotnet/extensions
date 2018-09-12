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
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorDocumentTracker } from './RazorDocumentTracker';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageConfiguration } from './RazorLanguageConfiguration';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { resolveRazorLanguageServerOptions } from './RazorLanguageServerOptionsResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorProjectTracker } from './RazorProjectTracker';
import { RazorSignatureHelpProvider } from './RazorSignatureHelpProvider';

let activationResolver: (value?: any) => void;
export const extensionActivated = new Promise(resolve => {
    activationResolver = resolve;
});

export async function activate(context: ExtensionContext) {
    const languageServerOptions = resolveRazorLanguageServerOptions();
    const languageConfiguration = new RazorLanguageConfiguration();
    const languageServerClient = new RazorLanguageServerClient(languageServerOptions);
    const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
    const documentManager = new RazorDocumentManager(languageServerClient);
    const csharpFeature = new RazorCSharpFeature(documentManager);
    const htmlFeature = new RazorHtmlFeature(documentManager, languageServiceClient);
    const projectTracker = new RazorProjectTracker(languageServiceClient);
    const documentTracker = new RazorDocumentTracker(documentManager, languageServiceClient);
    const localRegistrations: vscode.Disposable[] = [];

    const onStartRegistration = languageServerClient.onStart(() => {
        const documentSynchronizer = new RazorDocumentSynchronizer();
        const provisionalCompletionOrchestrator = new ProvisionalCompletionOrchestrator(
            documentManager,
            csharpFeature.projectionProvider,
            languageServiceClient);
        const completionItemProvider = new RazorCompletionItemProvider(
            documentSynchronizer,
            documentManager,
            languageServiceClient,
            provisionalCompletionOrchestrator);
        const signatureHelpProvider = new RazorSignatureHelpProvider(
            documentSynchronizer,
            documentManager,
            languageServiceClient);

        localRegistrations.push(
            languageConfiguration.register(),
            provisionalCompletionOrchestrator.register(),
            vscode.languages.registerCompletionItemProvider(
                RazorLanguage.id,
                completionItemProvider,
                '.', '<', '@'),
            vscode.languages.registerSignatureHelpProvider(
                RazorLanguage.id,
                signatureHelpProvider,
                '(', ','),
            projectTracker.register(),
            documentManager.register(),
            documentTracker.register(),
            csharpFeature.register(),
            htmlFeature.register(),
            documentSynchronizer.register());
    });

    const onStopRegistration = languageServerClient.onStop(() => {
        localRegistrations.forEach(r => r.dispose());
        localRegistrations.length = 0;
    });

    await languageServerClient.start();
    await projectTracker.initialize();
    await documentManager.initialize();

    context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration);

    activationResolver();
}
