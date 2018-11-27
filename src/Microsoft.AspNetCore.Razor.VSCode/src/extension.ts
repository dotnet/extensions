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
import { reportTelemetryForProjects } from './ProjectTelemetryListener';
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
import { RazorProjectManager } from './RazorProjectManager';
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
        const projectManager = new RazorProjectManager(logger);
        reportTelemetryForProjects(projectManager, telemetryReporter);
        const languageConfiguration = new RazorLanguageConfiguration();
        const csharpFeature = new RazorCSharpFeature(documentManager);
        const htmlFeature = new RazorHtmlFeature(documentManager, languageServiceClient);
        const projectTracker = new RazorProjectTracker(projectManager, languageServiceClient);
        const documentTracker = new RazorDocumentTracker(documentManager, languageServiceClient);
        const localRegistrations: vscode.Disposable[] = [];

        const onStartRegistration = languageServerClient.onStart(() => {
            const documentSynchronizer = new RazorDocumentSynchronizer(documentManager, logger);
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
                projectManager.register(),
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

        languageServerClient.onStarted(async () => {
            await projectManager.initialize();
            await documentManager.initialize();
        });

        await startLanguageServer(languageServerClient, logger, context);

        context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration, logger);
    } catch (error) {
        telemetryReporter.reportErrorOnActivation(error);
    }
}

async function startLanguageServer(
    languageServerClient: RazorLanguageServerClient,
    logger: RazorLogger,
    context: vscode.ExtensionContext) {

    const razorFiles = await vscode.workspace.findFiles(RazorLanguage.globbingPattern);
    if (razorFiles.length === 0) {
        // No Razor files in workspace, language server should stay off until one is added or opened.
        logger.logAlways('No Razor files detected in workspace, delaying language server start.');

        const watcher = vscode.workspace.createFileSystemWatcher(RazorLanguage.globbingPattern);
        const delayedLanguageServerStart = async () => {
            razorFileCreatedRegistration.dispose();
            razorFileOpenedRegistration.dispose();
            await languageServerClient.start();
        };
        const razorFileCreatedRegistration = watcher.onDidCreate(() => delayedLanguageServerStart());
        const razorFileOpenedRegistration = vscode.workspace.onDidOpenTextDocument(async (event) => {
            if (event.languageId === RazorLanguage.id) {
                await delayedLanguageServerStart();
            }
        });
        context.subscriptions.push(razorFileCreatedRegistration, razorFileOpenedRegistration);
    } else {
        await languageServerClient.start();
    }
}
