/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as cp from 'child_process';
import * as os from 'os';
import { IRazorDocument } from '../IRazorDocument';
import { IRazorDocumentManager } from '../IRazorDocumentManager';
import { razorExtensionId } from '../RazorExtensionId';
import * as vscode from '../vscodeAdapter';
import { IReportIssueDataCollectionResult } from './IReportIssueDataCollectionResult';

export class ReportIssueCreator {
    constructor(
        private readonly vscodeApi: vscode.api,
        private readonly documentManager: IRazorDocumentManager) {
    }

    public async create(collectionResult: IReportIssueDataCollectionResult) {
        let razorContent: string;
        let csharpContent: string;
        let htmlContent: string;

        if (collectionResult.document) {
            razorContent = await this.getRazor(collectionResult.document);

            const razorDocument = await this.documentManager.getDocument(collectionResult.document.uri);
            csharpContent = await this.getProjectedCSharp(razorDocument);
            htmlContent = await this.getProjectedHtml(razorDocument);
        } else {
            razorContent = 'Non Razor file as active document';
            csharpContent = 'Could not determine CSharp content';
            htmlContent = 'Could not determine Html content';
        }

        const razorExtensionVersion = this.getExtensionVersion();
        let dotnetInfo = '';
        try {
            dotnetInfo = await this.getDotnetInfo();
        } catch (error) {
            dotnetInfo = `A valid dotnet installation could not be found: ${error}`;
        }
        const extensionTable = this.generateExtensionTable();

        const sanitizedLogOutput = this.sanitize(collectionResult.logOutput);
        const sanitizedRazorContent = this.sanitize(razorContent);
        const sanitizedCSharpContent = this.sanitize(csharpContent);
        const sanitizedHtmlContent = this.sanitize(htmlContent);
        const sanitizedDotnetInfo = this.sanitize(dotnetInfo);
        return `## Is this a Bug or Feature request?:
Bug

## Steps to reproduce
------------------- Please fill in this section -------------------------

## Description of the problem:
------------------- Please fill in this section -------------------------

Expected behavior:

Actual behavior:

## Logs

#### OmniSharp
------------------- Please fill in this section -------------------------
To find the OmniSharp log, open VS Code's "Output" pane, then in the dropdown choose "OmniSharp Log".

#### Razor
<details><summary>Expand</summary>
<p>

\`\`\`
${sanitizedLogOutput}
\`\`\`

</p>
</details>

## Workspace information

#### Razor document:
<details><summary>Expand</summary>
<p>

\`\`\`Razor
${sanitizedRazorContent}
\`\`\`

</p>
</details>

#### Projected CSharp document:
<details><summary>Expand</summary>
<p>

\`\`\`C#
${sanitizedCSharpContent}
\`\`\`

</p>
</details>

#### Projected Html document:
<details><summary>Expand</summary>
<p>

\`\`\`Html
${sanitizedHtmlContent}
\`\`\`

</p>
</details>

## Machine information


**VSCode version**: ${this.vscodeApi.version}
**Razor.VSCode version**: ${razorExtensionVersion}
#### \`dotnet --info\`

<details><summary>Expand</summary>
<p>

\`\`\`
${sanitizedDotnetInfo}
\`\`\`

</p>
</details>

#### Extensions
<details><summary>Expand</summary>
<p>

${extensionTable}

</p>
</details>`;
    }

    // Protected for testing
    protected sanitize(content: string) {
        const user = process.env.USERNAME === undefined ? process.env.USER : process.env.USERNAME;

        if (user === undefined) {
            // Couldn't determine user, therefore can't truly sanitize the content.
            return content;
        }

        const replacer = new RegExp(user, 'g');
        const sanitizedContent = content.replace(replacer, 'anonymous');
        return sanitizedContent;
    }

    // Protected for testing
    protected async getRazor(document: vscode.TextDocument) {
        const content = document.getText();

        return content;
    }

    // Protected for testing
    protected async getProjectedCSharp(razorDocument: IRazorDocument) {
        let csharpContent = `////////////////////// Projected CSharp as seen by extension ///////////////////////
${razorDocument.csharpDocument.getContent()}


////////////////////// Projected CSharp as seen by VSCode ///////////////////////`;

        const csharpTextDocument = await this.vscodeApi.workspace.openTextDocument(razorDocument.csharpDocument.uri);
        if (csharpTextDocument) {
            csharpContent = `${csharpContent}
${csharpTextDocument.getText()}`;
        } else {
            csharpContent = `${csharpContent}
Unable to resolve VSCode's version of CSharp`;
        }

        return csharpContent;
    }

    // Protected for testing
    protected async getProjectedHtml(razorDocument: IRazorDocument) {
        let htmlContent = `////////////////////// Projected Html as seen by extension ///////////////////////
${razorDocument.htmlDocument.getContent()}


////////////////////// Projected Html as seen by VSCode ///////////////////////`;

        const htmlTextDocument = await this.vscodeApi.workspace.openTextDocument(razorDocument.htmlDocument.uri);
        if (htmlTextDocument) {
            htmlContent = `${htmlContent}
${htmlTextDocument.getText()}`;
        } else {
            htmlContent = `${htmlContent}
Unable to resolve VSCode's version of Html`;
        }

        return htmlContent;
    }

    // Protected for testing
    protected getExtensionVersion(): string {
        const extension = this.vscodeApi.extensions.getExtension(razorExtensionId);
        if (!extension) {
            return 'Unable to find Razor extension version.';
        }
        return extension.packageJSON.version;
    }

    // Protected for testing
    protected getInstalledExtensions() {
        const extensions: Array<vscode.Extension<any>> = this.vscodeApi.extensions.all
            .filter(extension => extension.packageJSON.isBuiltin === false);

        return extensions.sort((a, b) =>
            a.packageJSON.name.toLowerCase().localeCompare(b.packageJSON.name.toLowerCase()));
    }

    // Protected for testing
    protected generateExtensionTable() {
        const extensions = this.getInstalledExtensions();
        if (extensions.length <= 0) {
            return 'none';
        }

        const tableHeader = `|Extension|Author|Version|${os.EOL}|---|---|---|`;
        const table = extensions.map(
            (e) => `|${e.packageJSON.name}|${e.packageJSON.publisher}|${e.packageJSON.version}|`).join(os.EOL);

        const extensionTable = `
${tableHeader}${os.EOL}${table};
`;

        return extensionTable;
    }

    private getDotnetInfo(): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            try {
                cp.exec('dotnet --info', { cwd: process.cwd(), maxBuffer: 500 * 1024 }, (error, stdout, stderr) => {
                    if (error) {
                        reject(error);
                    } else if (stderr && stderr.length > 0) {
                        reject(error);
                    } else {
                        resolve(stdout);
                    }
                });
            } catch (error) {
                reject(error);
            }
        });
    }
}
