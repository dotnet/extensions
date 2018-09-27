/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { reportTelemetryForDocuments } from './DocumentTelemetryListener';
import { HostEventStream } from './HostEventStream';
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
import { resolveRazorLanguageServerTrace } from './RazorLanguageServerTraceResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { RazorProjectTracker } from './RazorProjectTracker';
import { RazorSignatureHelpProvider } from './RazorSignatureHelpProvider';
import { TelemetryReporter } from './TelemetryReporter';

export async function activate(context: ExtensionContext, languageServerDir: string, eventStream: HostEventStream) {
    const telemetryReporter = new TelemetryReporter(eventStream);
    try {
        const languageServerTrace = resolveRazorLanguageServerTrace();
        const logger = new RazorLogger(languageServerTrace);
        const languageServerOptions = resolveRazorLanguageServerOptions(languageServerDir, languageServerTrace, logger);
        const languageServerClient = new RazorLanguageServerClient(languageServerOptions, telemetryReporter, logger);
        const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);
        const documentManager = new RazorDocumentManager(languageServerClient, logger);
        reportTelemetryForDocuments(documentManager, telemetryReporter);
        const languageConfiguration = new RazorLanguageConfiguration();
        const csharpFeature = new RazorCSharpFeature(documentManager);
        const htmlFeature = new RazorHtmlFeature(documentManager, languageServiceClient);
        const projectTracker = new RazorProjectTracker(languageServiceClient);
        const documentTracker = new RazorDocumentTracker(documentManager, languageServiceClient);
        const localRegistrations: vscode.Disposable[] = [];

        const onStartRegistration = languageServerClient.onStart(() => {
            const documentSynchronizer = new RazorDocumentSynchronizer(logger);
            const provisionalCompletionOrchestrator = new ProvisionalCompletionOrchestrator(
                documentManager,
                csharpFeature.projectionProvider,
                languageServiceClient,
                logger);
            const completionItemProvider = new RazorCompletionItemProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                provisionalCompletionOrchestrator,
                logger);
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

        context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration, logger);
    } catch (error) {
        telemetryReporter.reportErrorOnActivation(error);
    }
}
