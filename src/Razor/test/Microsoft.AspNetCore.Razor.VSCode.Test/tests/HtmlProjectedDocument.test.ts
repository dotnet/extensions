/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { HtmlProjectedDocument } from 'microsoft.aspnetcore.razor.vscode/dist/Html/HtmlProjectedDocument';
import { ServerTextChange } from 'microsoft.aspnetcore.razor.vscode/dist/RPC/ServerTextChange';
import { createTestVSCodeApi } from './Mocks/TestVSCodeApi';

describe('HtmlProjectedDocument', () => {

    it('reset clears state', () => {
        // Arrange
        const api = createTestVSCodeApi();
        const htmlDocumentUri = api.Uri.parse('C:/path/to/file.cshtml.__virtual.html');
        const htmlDocument = new HtmlProjectedDocument(htmlDocumentUri);
        const edit: ServerTextChange = {
            newText: 'Hello World',
            span: {
                start: 0,
                length: 11,
            },
        };
        htmlDocument.update([edit], 1337);

        // Act
        htmlDocument.reset();

        // Assert
        assert.equal(htmlDocument.hostDocumentSyncVersion, null);
        assert.equal(htmlDocument.getContent(), '');
    });
});
