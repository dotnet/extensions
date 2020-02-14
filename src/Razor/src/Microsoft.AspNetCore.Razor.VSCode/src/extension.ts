/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { ExtensionContext } from 'vscode';
import { CompositeCodeActionTranslator } from './CodeActions/CompositeRazorCodeActionTranslator';
import { RazorCodeActionProvider } from './CodeActions/RazorCodeActionProvider';
import { RazorFullyQualifiedCodeActionTranslator } from './CodeActions/RazorFullyQualifiedCodeActionTranslator';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { ReportIssueCommand } from './Diagnostics/ReportIssueCommand';
import { reportTelemetryForDocuments } from './DocumentTelemetryListener';
import { HostEventStream } from './HostEventStream';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { IEventEmitterFactory } from './IEventEmitterFactory';
import { ProvisionalCompletionOrchestrator } from './ProvisionalCompletionOrchestrator';
import { RazorCodeLensProvider } from './RazorCodeLensProvider';
import { RazorCompletionItemProvider } from './RazorCompletionItemProvider';
import { RazorCSharpLanguageMiddleware } from './RazorCSharpLanguageMiddleware';
import { RazorDefinitionProvider } from './RazorDefinitionProvider';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorFormattingFeature } from './RazorFormattingFeature';
import { RazorHoverProvider } from './RazorHoverProvider';
import { RazorImplementationProvider } from './RazorImplementationProvider';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageConfiguration } from './RazorLanguageConfiguration';
import { RazorLanguageServerClient } from './RazorLanguageServerClient';
import { resolveRazorLanguageServerOptions } from './RazorLanguageServerOptionsResolver';
import { resolveRazorLanguageServerTrace } from './RazorLanguageServerTraceResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { RazorReferenceProvider } from './RazorReferenceProvider';
import { RazorRenameProvider } from './RazorRenameProvider';
import { RazorSignatureHelpProvider } from './RazorSignatureHelpProvider';
import { TelemetryReporter } from './TelemetryReporter';

export async function activate(context: ExtensionContext, languageServerDir: string, eventStream: HostEventStream) {
    const telemetryReporter = new TelemetryReporter(eventStream);
    const eventEmitterFactory: IEventEmitterFactory = {
        create: <T>() => new vscode.EventEmitter<T>(),
    };
    const languageServerTrace = resolveRazorLanguageServerTrace(vscode);
    const logger = new RazorLogger(vscode, eventEmitterFactory, languageServerTrace);
    try {
        const languageServerOptions = resolveRazorLanguageServerOptions(vscode, languageServerDir, languageServerTrace, logger);
        const languageServerClient = new RazorLanguageServerClient(languageServerOptions, telemetryReporter, logger);
        const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);

        const codeActionTranslators = [
            new RazorFullyQualifiedCodeActionTranslator(),
        ];
        const compositeCodeActionTranslator = new CompositeCodeActionTranslator(codeActionTranslators);

        const razorLanguageMiddleware = new RazorCSharpLanguageMiddleware(languageServiceClient, logger, compositeCodeActionTranslator);

        const documentManager = new RazorDocumentManager(languageServerClient, logger);
        reportTelemetryForDocuments(documentManager, telemetryReporter);
        const languageConfiguration = new RazorLanguageConfiguration();
        const csharpFeature = new RazorCSharpFeature(documentManager, eventEmitterFactory, logger);
        const htmlFeature = new RazorHtmlFeature(documentManager, languageServiceClient, eventEmitterFactory, logger);
        const localRegistrations: vscode.Disposable[] = [];
        const reportIssueCommand = new ReportIssueCommand(vscode, documentManager, logger);
        const razorFormattingFeature = new RazorFormattingFeature(languageServerClient, documentManager, logger);

        const onStartRegistration = languageServerClient.onStart(() => {
            vscode.commands.executeCommand<void>('omnisharp.registerLanguageMiddleware', razorLanguageMiddleware);
            const documentSynchronizer = new RazorDocumentSynchronizer(documentManager, logger);
            const provisionalCompletionOrchestrator = new ProvisionalCompletionOrchestrator(
                documentManager,
                csharpFeature.projectionProvider,
                languageServiceClient,
                logger);
            const codeActionProvider = new RazorCodeActionProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger,
                compositeCodeActionTranslator);
            const completionItemProvider = new RazorCompletionItemProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                provisionalCompletionOrchestrator,
                logger);
            const signatureHelpProvider = new RazorSignatureHelpProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const definitionProvider = new RazorDefinitionProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const implementationProvider = new RazorImplementationProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const hoverProvider = new RazorHoverProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const codeLensProvider = new RazorCodeLensProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const renameProvider = new RazorRenameProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            const referenceProvider = new RazorReferenceProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);

            localRegistrations.push(
                languageConfiguration.register(),
                provisionalCompletionOrchestrator.register(),
                vscode.languages.registerCodeActionsProvider(
                    RazorLanguage.id,
                    codeActionProvider),
                vscode.languages.registerCompletionItemProvider(
                    RazorLanguage.id,
                    completionItemProvider,
                    '.', '<', '@'),
                vscode.languages.registerSignatureHelpProvider(
                    RazorLanguage.id,
                    signatureHelpProvider,
                    '(', ','),
                vscode.languages.registerDefinitionProvider(
                    RazorLanguage.id,
                    definitionProvider),
                vscode.languages.registerImplementationProvider(
                    RazorLanguage.id,
                    implementationProvider),
                vscode.languages.registerHoverProvider(
                    RazorLanguage.documentSelector,
                    hoverProvider),
                vscode.languages.registerReferenceProvider(
                    RazorLanguage.id,
                    referenceProvider),
                vscode.languages.registerCodeLensProvider(
                    RazorLanguage.id,
                    codeLensProvider),
                vscode.languages.registerRenameProvider(
                    RazorLanguage.id,
                    renameProvider),
                documentManager.register(),
                csharpFeature.register(),
                htmlFeature.register(),
                documentSynchronizer.register(),
                reportIssueCommand.register());

            razorFormattingFeature.register();
        });

        const onStopRegistration = languageServerClient.onStop(() => {
            localRegistrations.forEach(r => r.dispose());
            localRegistrations.length = 0;
        });

        languageServerClient.onStarted(async () => {
            await documentManager.initialize();
        });

        await startLanguageServer(languageServerClient, logger, context);

        context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration, logger);
    } catch (error) {
        logger.logError('Failed when activating Razor VSCode.', error);
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
