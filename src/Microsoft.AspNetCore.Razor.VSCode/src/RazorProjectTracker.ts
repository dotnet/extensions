/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IRazorProjectChangeEvent } from './IRazorProjectChangeEvent';
import { RazorLanguageServiceClient } from './RazorLanguageServiceClient';
import { RazorProjectChangeKind } from './RazorProjectChangeKind';
import { RazorProjectManager } from './RazorProjectManager';

export class RazorProjectTracker {
    constructor(
        private readonly razorProjectManager: RazorProjectManager,
        private readonly languageServiceClient: RazorLanguageServiceClient) {
    }

    public register() {
        const registration = this.razorProjectManager.onChange(event => this.onProjectChange(event));

        return registration;
    }

    private async onProjectChange(event: IRazorProjectChangeEvent) {
        if (event.kind === RazorProjectChangeKind.added) {
            await this.languageServiceClient.addProject(event.project.uri);
        } else if (event.kind === RazorProjectChangeKind.removed) {
            await this.languageServiceClient.removeProject(event.project.uri);
        } else if (event.kind === RazorProjectChangeKind.changed) {
            const projectConfiguration = event.project.configuration;

            if (!projectConfiguration) {
                return;
            }

            await this.languageServiceClient.updateProject(projectConfiguration);
        }
    }
}
