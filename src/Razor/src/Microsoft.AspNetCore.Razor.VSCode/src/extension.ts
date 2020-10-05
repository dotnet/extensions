/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import * as vscodeapi from 'vscode';
import { ExtensionContext } from 'vscode';
import { BlazorDebugConfigurationProvider } from './BlazorDebug/BlazorDebugConfigurationProvider';
import { CodeActionsHandler } from './CodeActions/CodeActionsHandler';
import { RazorCodeActionRunner } from './CodeActions/RazorCodeActionRunner';
import { listenToConfigurationChanges } from './ConfigurationChangeListener';
import { RazorCSharpFeature } from './CSharp/RazorCSharpFeature';
import { ReportIssueCommand } from './Diagnostics/ReportIssueCommand';
import { reportTelemetryForDocuments } from './DocumentTelemetryListener';
import { HostEventStream } from './HostEventStream';
import { RazorHtmlFeature } from './Html/RazorHtmlFeature';
import { IEventEmitterFactory } from './IEventEmitterFactory';
import { ProposedApisFeature } from './ProposedApisFeature';
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
import { resolveRazorLanguageServerTrace } from './RazorLanguageServerTraceResolver';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { RazorReferenceProvider } from './RazorReferenceProvider';
import { RazorRenameProvider } from './RazorRenameProvider';
import { RazorSignatureHelpProvider } from './RazorSignatureHelpProvider';
import { RazorDocumentSemanticTokensProvider } from './Semantic/RazorDocumentSemanticTokensProvider';
import { SemanticTokensHandler } from './Semantic/SemanticTokensHandler';
import { TelemetryReporter } from './TelemetryReporter';

// We specifically need to take a reference to a particular instance of the vscode namespace,
// otherwise providers attempt to operate on the null extension.
export async function activate(vscodeType: typeof vscodeapi, context: ExtensionContext, languageServerDir: string, eventStream: HostEventStream, enableProposedApis = false) {
    const telemetryReporter = new TelemetryReporter(eventStream);
    const eventEmitterFactory: IEventEmitterFactory = {
        create: <T>() => new vscode.EventEmitter<T>(),
    };

    const languageServerTrace = resolveRazorLanguageServerTrace(vscodeType);
    const logger = new RazorLogger(vscodeType, eventEmitterFactory, languageServerTrace);

    try {
        const languageServerClient = new RazorLanguageServerClient(vscodeType, languageServerDir, telemetryReporter, logger);
        const languageServiceClient = new RazorLanguageServiceClient(languageServerClient);

        const razorLanguageMiddleware = new RazorCSharpLanguageMiddleware(languageServiceClient, logger);

        const documentManager = new RazorDocumentManager(languageServerClient, logger);
        reportTelemetryForDocuments(documentManager, telemetryReporter);
        const languageConfiguration = new RazorLanguageConfiguration();
        const csharpFeature = new RazorCSharpFeature(documentManager, eventEmitterFactory, logger);
        const htmlFeature = new RazorHtmlFeature(documentManager, languageServiceClient, eventEmitterFactory, logger);
        const localRegistrations: vscode.Disposable[] = [];
        const reportIssueCommand = new ReportIssueCommand(vscodeType, documentManager, logger);
        const razorFormattingFeature = new RazorFormattingFeature(languageServerClient, documentManager, logger);
        const razorCodeActionRunner = new RazorCodeActionRunner(languageServerClient, logger);

        const onStartRegistration = languageServerClient.onStart(async () => {
            vscodeType.commands.executeCommand<void>('omnisharp.registerLanguageMiddleware', razorLanguageMiddleware);
            const documentSynchronizer = new RazorDocumentSynchronizer(documentManager, logger);
            const provisionalCompletionOrchestrator = new ProvisionalCompletionOrchestrator(
                documentManager,
                csharpFeature.projectionProvider,
                languageServiceClient,
                logger);
            const codeActionHandler = new CodeActionsHandler(
                documentManager,
                languageServerClient,
                logger);
            const semanticTokenHandler = new SemanticTokensHandler(languageServerClient);
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
                vscodeType.languages.registerCompletionItemProvider(
                    RazorLanguage.id,
                    completionItemProvider,
                    '.', '<', '@'),
                vscodeType.languages.registerSignatureHelpProvider(
                    RazorLanguage.id,
                    signatureHelpProvider,
                    '(', ','),
                vscodeType.languages.registerDefinitionProvider(
                    RazorLanguage.id,
                    definitionProvider),
                vscodeType.languages.registerImplementationProvider(
                    RazorLanguage.id,
                    implementationProvider),
                vscodeType.languages.registerHoverProvider(
                    RazorLanguage.documentSelector,
                    hoverProvider),
                vscodeType.languages.registerReferenceProvider(
                    RazorLanguage.id,
                    referenceProvider),
                vscodeType.languages.registerCodeLensProvider(
                    RazorLanguage.id,
                    codeLensProvider),
                vscodeType.languages.registerRenameProvider(
                    RazorLanguage.id,
                    renameProvider),
                documentManager.register(),
                csharpFeature.register(),
                htmlFeature.register(),
                documentSynchronizer.register(),
                reportIssueCommand.register(),
                listenToConfigurationChanges(languageServerClient));

            const legend = await languageServiceClient.getSemanticTokenLegend();
            const semanticTokenProvider = new RazorDocumentSemanticTokensProvider(
                documentSynchronizer,
                documentManager,
                languageServiceClient,
                logger);
            if (legend) {
                localRegistrations.push(vscodeType.languages.registerDocumentSemanticTokensProvider(RazorLanguage.id, semanticTokenProvider, legend));
                localRegistrations.push(vscodeType.languages.registerDocumentRangeSemanticTokensProvider(RazorLanguage.id, semanticTokenProvider, legend));
            }

            if (enableProposedApis) {
                const proposedApisFeature = new ProposedApisFeature();

                await proposedApisFeature.register(vscodeType, localRegistrations);
            }

            razorFormattingFeature.register();
            razorCodeActionRunner.register();
            codeActionHandler.register();
            semanticTokenHandler.register();
        });

        const onStopRegistration = languageServerClient.onStop(() => {
            localRegistrations.forEach(r => r.dispose());
            localRegistrations.length = 0;
        });

        const provider = new BlazorDebugConfigurationProvider(logger, vscodeType);
        context.subscriptions.push(vscodeType.debug.registerDebugConfigurationProvider('blazorwasm', provider));

        languageServerClient.onStarted(async () => {
            await documentManager.initialize();
        });

        await startLanguageServer(vscodeType, languageServerClient, logger, context);

        context.subscriptions.push(languageServerClient, onStartRegistration, onStopRegistration, logger);
    } catch (error) {
        logger.logError('Failed when activating Razor VSCode.', error);
        telemetryReporter.reportErrorOnActivation(error);
    }
}

async function startLanguageServer(
    vscodeType: typeof vscodeapi,
    languageServerClient: RazorLanguageServerClient,
    logger: RazorLogger,
    context: vscode.ExtensionContext) {

    const razorFiles = await vscodeType.workspace.findFiles(RazorLanguage.globbingPattern);
    if (razorFiles.length === 0) {
        // No Razor files in workspace, language server should stay off until one is added or opened.
        logger.logAlways('No Razor files detected in workspace, delaying language server start.');

        const watcher = vscodeType.workspace.createFileSystemWatcher(RazorLanguage.globbingPattern);
        const delayedLanguageServerStart = async () => {
            razorFileCreatedRegistration.dispose();
            razorFileOpenedRegistration.dispose();
            await languageServerClient.start();
        };
        const razorFileCreatedRegistration = watcher.onDidCreate(() => delayedLanguageServerStart());
        const razorFileOpenedRegistration = vscodeType.workspace.onDidOpenTextDocument(async (event) => {
            if (event.languageId === RazorLanguage.id) {
                await delayedLanguageServerStart();
            }
        });
        context.subscriptions.push(razorFileCreatedRegistration, razorFileOpenedRegistration);
    } else {
        await languageServerClient.start();
    }
}
