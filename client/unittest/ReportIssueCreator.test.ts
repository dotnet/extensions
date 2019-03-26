/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { IReportIssueDataCollectionResult } from 'microsoft.aspnetcore.razor.vscode/dist/Diagnostics/IReportIssueDataCollectionResult';
import { ReportIssueCreator } from 'microsoft.aspnetcore.razor.vscode/dist/Diagnostics/ReportIssueCreator';
import { IRazorDocument } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocument';
import { IRazorDocumentManager } from 'microsoft.aspnetcore.razor.vscode/dist/IRazorDocumentManager';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import { TestProjectedDocument } from './Mocks/TestProjectedDocument';
import { TestRazorDocument } from './Mocks/TestRazorDocument';
import { TestRazorDocumentManager } from './Mocks/TestRazorDocumentManager';
import { TestTextDocument } from './Mocks/TestTextDocument';
import { createTestVSCodeApi } from './Mocks/TestVSCodeApi';

describe('ReportIssueCreator', () => {
    function getReportIssueCreator(api: vscode.api) {
        const documentManager = new TestRazorDocumentManager();
        const issueCreator = new TestReportIssueCreator(api, documentManager);
        return issueCreator;
    }

    it('sanitize replaces USERNAME with anonymous', () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);
        const user = 'JohnDoe';
        const content = `Hello ${user} World ${user}`;
        delete process.env.USER;
        process.env.USERNAME = user;

        // Act
        const sanitizedContent = issueCreator.sanitize(content);

        // Assert
        assert.equal('Hello anonymous World anonymous', sanitizedContent);
    });

    it('sanitize replaces USER with anonymous', () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);
        const user = 'JohnDoe';
        const content = `Hello ${user} World ${user}`;
        process.env.USER = user;
        delete process.env.USERNAME;

        // Act
        const sanitizedContent = issueCreator.sanitize(content);

        // Assert
        assert.equal('Hello anonymous World anonymous', sanitizedContent);
    });

    it('sanitize returns original content when no user', () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);
        const content = 'original content';
        delete process.env.USER;
        delete process.env.USERNAME;

        // Act
        const sanitizedContent = issueCreator.sanitize(content);

        // Assert
        assert.equal(sanitizedContent, content);
    });

    it('create can operate when no content is available', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);
        const collectionResult: IReportIssueDataCollectionResult = {
            document: undefined,
            logOutput: '',
        };

        // Act
        const issueContent = await issueCreator.create(collectionResult);

        // Assert
        assert.ok(issueContent.indexOf('Bug') > 0);
    });

    it('getRazor returns text documents contents', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);
        const expectedContent = 'TextDocument content';
        const textDocument = new TestTextDocument(expectedContent, api.Uri.parse('C:/path/to/file.cshtml'));

        // Act
        const razorContent = await issueCreator.getRazor(textDocument);

        // Assert
        assert.equal(razorContent, expectedContent);
    });

    it('getProjectedCSharp returns projected CSharp and vscodes text document CSharp', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const expectedVSCodeCSharpContent = 'VSCode seen CSharp content';
        const csharpDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.cs');
        const csharpTextDocument = new TestTextDocument(expectedVSCodeCSharpContent, csharpDocumentUri);
        api.setWorkspaceDocuments(csharpTextDocument);
        const hostDocumentUri = api.Uri.parse('C:/path/to/file.cshtml');
        const expectedProjectedCSharpContent = 'Projected CSharp content';
        const razorDocument = new TestRazorDocument(hostDocumentUri, hostDocumentUri.path);
        razorDocument.csharpDocument = new TestProjectedDocument(expectedProjectedCSharpContent, csharpDocumentUri);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const razorContent = await issueCreator.getProjectedCSharp(razorDocument);

        // Assert
        assert.ok(razorContent.indexOf(expectedVSCodeCSharpContent) > 0);
        assert.ok(razorContent.indexOf(expectedProjectedCSharpContent) > 0);
    });

    it('getProjectedCSharp returns only projected CSharp if cannot locate vscodes text document', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const csharpDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.cs');
        const hostDocumentUri = api.Uri.parse('C:/path/to/file.cshtml');
        const expectedProjectedCSharpContent = 'Projected CSharp content';
        const razorDocument = new TestRazorDocument(hostDocumentUri, hostDocumentUri.path);
        razorDocument.csharpDocument = new TestProjectedDocument(expectedProjectedCSharpContent, csharpDocumentUri);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const razorContent = await issueCreator.getProjectedCSharp(razorDocument);

        // Assert
        assert.ok(razorContent.indexOf(expectedProjectedCSharpContent) > 0);
    });

    it('getProjectedHtml returns projected Html and vscodes text document Html', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const expectedVSCodeHtmlContent = 'VSCode seen Html content';
        const htmlDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.cs');
        const htmlTextDocument = new TestTextDocument(expectedVSCodeHtmlContent, htmlDocumentUri);
        api.setWorkspaceDocuments(htmlTextDocument);
        const hostDocumentUri = api.Uri.parse('C:/path/to/file.cshtml');
        const expectedProjectedHtmlContent = 'Projected Html content';
        const razorDocument = new TestRazorDocument(hostDocumentUri, hostDocumentUri.path);
        razorDocument.htmlDocument = new TestProjectedDocument(expectedProjectedHtmlContent, htmlDocumentUri);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const razorContent = await issueCreator.getProjectedHtml(razorDocument);

        // Assert
        assert.ok(razorContent.indexOf(expectedVSCodeHtmlContent) > 0);
        assert.ok(razorContent.indexOf(expectedProjectedHtmlContent) > 0);
    });

    it('getProjectedHtml returns only projected Html if cannot locate vscodes text document', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const htmlDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.html');
        const hostDocumentUri = api.Uri.parse('C:/path/to/file.cshtml');
        const expectedProjectedHtmlContent = 'Projected Html content';
        const razorDocument = new TestRazorDocument(hostDocumentUri, hostDocumentUri.path);
        razorDocument.htmlDocument = new TestProjectedDocument(expectedProjectedHtmlContent, htmlDocumentUri);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const razorContent = await issueCreator.getProjectedHtml(razorDocument);

        // Assert
        assert.ok(razorContent.indexOf(expectedProjectedHtmlContent) > 0);
    });

    const omniSharpExtension: vscode.Extension<any> = {
        id: 'ms-vscode.csharp',
        packageJSON: {
            name: 'OmniSharp',
            version: '1234',
            isBuiltin: false,
        },
    };
    const razorClientExtension: vscode.Extension<any> = {
        id: 'ms-vscode.razor-vscode',
        packageJSON: {
            name: 'Razor',
            version: '5678',
            isBuiltin: false,
        },
    };

    it('getExtensionVersion returns OmniSharp extension version', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        api.setExtensions(omniSharpExtension, razorClientExtension);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const extensionVersion = issueCreator.getExtensionVersion();

        // Assert
        assert.equal(extensionVersion, '1234');
    });

    it('getExtensionVersion can not find extension', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);

        // Act & Assert
        assert.doesNotThrow(async () => issueCreator.getExtensionVersion());
    });

    it('getInstalledExtensions returns non built-in extensions sorted by name', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const builtinExtension: vscode.Extension<any> = {
            id: 'something.builtin',
            packageJSON: {
                name: 'BuiltInThing',
                isBuiltin: true,
            },
        };
        api.setExtensions(razorClientExtension, builtinExtension, omniSharpExtension);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const extensions = issueCreator.getInstalledExtensions();

        // Assert
        assert.deepEqual(extensions, [omniSharpExtension, razorClientExtension]);
    });

    it('generateExtensionTable returns all non-builtin extensions in string format', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const builtinExtension: vscode.Extension<any> = {
            id: 'something.builtin',
            packageJSON: {
                name: 'BuiltInThing',
                version: 'ShouldNotShowUp',
                isBuiltin: true,
            },
        };
        api.setExtensions(razorClientExtension, builtinExtension, omniSharpExtension);
        const issueCreator = getReportIssueCreator(api);

        // Act
        const table = issueCreator.generateExtensionTable();

        // Assert
        assert.ok(table.indexOf(omniSharpExtension.packageJSON.version) > 0);
        assert.ok(table.indexOf(razorClientExtension.packageJSON.version) > 0);
        assert.ok(table.indexOf(builtinExtension.packageJSON.version) === -1);
    });

    it('generateExtensionTable can operate when 0 extensions', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const issueCreator = getReportIssueCreator(api);

        // Act & Assert
        assert.doesNotThrow(() => issueCreator.generateExtensionTable());
    });
});

class TestReportIssueCreator extends ReportIssueCreator {
    constructor(vscodeApi: vscode.api, documentManager: IRazorDocumentManager) {
        super(vscodeApi, documentManager);
    }

    public getRazor(document: vscode.TextDocument) {
        return super.getRazor(document);
    }

    public getProjectedCSharp(razorDocument: IRazorDocument) {
        return super.getProjectedCSharp(razorDocument);
    }

    public getProjectedHtml(razorDocument: IRazorDocument) {
        return super.getProjectedHtml(razorDocument);
    }

    public getExtensionVersion() {
        return super.getExtensionVersion();
    }

    public getInstalledExtensions() {
        return super.getInstalledExtensions();
    }

    public generateExtensionTable() {
        return super.generateExtensionTable();
    }

    public sanitize(content: string) {
        return super.sanitize(content);
    }
}
