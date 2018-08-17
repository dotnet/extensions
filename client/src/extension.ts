/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorDocumentTracker } from './RazorDocumentTracker';
import { RazorLanguage } from './RazorLanguage';
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
    const languageServerClient = new RazorLanguageServerClient(languageServerOptions);
    const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
    const csharpFeature = new RazorCSharpFeature(languageServerClient);
    const projectTracker = new RazorProjectTracker(languageServiceClient);
    const documentTracker = new RazorDocumentTracker(languageServiceClient);
    const localRegistrations: vscode.Disposable[] = [];

    const onStartRegistration = languageServerClient.onStart(() => {
        localRegistrations.push(
            vscode.languages.registerCompletionItemProvider(
                RazorLanguage.id,
                new RazorCompletionItemProvider(csharpFeature, languageServiceClient)),
            projectTracker.register(),
            documentTracker.register(),
            csharpFeature.register());
    });

    const onStopRegistration = languageServerClient.onStop(() => {
        localRegistrations.forEach(r => r.dispose());
        localRegistrations.length = 0;
    });

    await languageServerClient.start();
    await projectTracker.initialize();
    await documentTracker.initialize();

    context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration);

    activationResolver();
}
