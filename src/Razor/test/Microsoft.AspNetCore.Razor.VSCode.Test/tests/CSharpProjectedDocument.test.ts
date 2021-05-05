/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { CSharpProjectedDocument } from 'microsoft.aspnetcore.razor.vscode/dist/CSharp/CSharpProjectedDocument';
import { ServerTextChange } from 'microsoft.aspnetcore.razor.vscode/dist/RPC/ServerTextChange';
import { createTestVSCodeApi } from './Mocks/TestVSCodeApi';

describe('CSharpProjectedDocument', () => {

    it('reset clears state', () => {
        // Arrange
        const api = createTestVSCodeApi();
        const csharpDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.cs');
        const csharpDocument = new CSharpProjectedDocument(csharpDocumentUri);
        const edit: ServerTextChange = {
            newText: 'Hello World',
            span: {
                start: 0,
                length: 11,
            },
        };
        csharpDocument.update([edit], 1337);

        // Act
        csharpDocument.reset();

        // Assert
        assert.equal(csharpDocument.hostDocumentSyncVersion, null);
        assert.equal(csharpDocument.getContent(), '');
    });
});
