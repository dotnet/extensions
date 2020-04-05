/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import * as vscodeapi from 'vscode';
import { RazorDocumentManager } from './RazorDocumentManager';
import { RazorDocumentSynchronizer } from './RazorDocumentSynchronizer';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorLogger } from './RazorLogger';
import { RazorDocumentSemanticTokensProvider } from './Semantic/RazorDocumentSemanticTokensProvider';
export class ProposedApisFeature {
    constructor(
        private documentSynchronizer: RazorDocumentSynchronizer,
        private documentManager: RazorDocumentManager,
        private languageServiceClient: RazorLanguageServiceClient,
        private logger: RazorLogger,
    ) {
    }

    public async register(vscodeType: typeof vscodeapi, localRegistrations: vscode.Disposable[]) {
        if (vscodeType.env.appName.endsWith('Insiders')) {
            const legend = await this.languageServiceClient.getSemanticTokenLegend();
            const semanticTokenProvider = new RazorDocumentSemanticTokensProvider(this.documentSynchronizer, this.documentManager, this.languageServiceClient, this.logger);
            if (legend) {
                localRegistrations.push(vscodeType.languages.registerDocumentSemanticTokensProvider(RazorLanguage.id, semanticTokenProvider, legend));
            }
        }
    }
}
