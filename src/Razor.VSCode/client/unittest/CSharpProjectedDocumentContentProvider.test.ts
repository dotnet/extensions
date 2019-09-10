/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { CSharpProjectedDocumentContentProvider } from 'microsoft.aspnetcore.razor.vscode/dist/CSharp/CSharpProjectedDocumentContentProvider';
import { RazorLogger } from 'microsoft.aspnetcore.razor.vscode/dist/RazorLogger';
import { Trace } from 'microsoft.aspnetcore.razor.vscode/dist/Trace';
import { TestEventEmitterFactory } from './Mocks/TestEventEmitterFactory';
import { TestRazorDocumentManager } from './Mocks/TestRazorDocumentManager';
import { createTestVSCodeApi } from './Mocks/TestVSCodeApi';

describe('CSharpProjectedDocumentContentProvider', () => {

    it('provideTextDocumentContent returns empty string for unknown document', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const eventEmitterFactory = new TestEventEmitterFactory();
        const provider = new CSharpProjectedDocumentContentProvider(
            new TestRazorDocumentManager(),
            eventEmitterFactory,
            new RazorLogger(api, eventEmitterFactory, Trace.Off));
        const csharpDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.cs');

        // Act
        const result = await provider.provideTextDocumentContent(csharpDocumentUri);

        // Assert
        assert.equal(result, '');
    });
});
