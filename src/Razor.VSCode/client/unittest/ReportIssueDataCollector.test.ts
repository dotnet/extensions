/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { ReportIssueDataCollector } from 'microsoft.aspnetcore.razor.vscode/dist/Diagnostics/ReportIssueDataCollector';
import { RazorLogger } from 'microsoft.aspnetcore.razor.vscode/dist/RazorLogger';
import { Trace } from 'microsoft.aspnetcore.razor.vscode/dist/Trace';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import { TestEventEmitterFactory } from './Mocks/TestEventEmitterFactory';
import { TestTextDocument } from './Mocks/TestTextDocument';
import { createTestVSCodeApi } from './Mocks/TestVSCodeApi';

describe('ReportIssueDataCollector', () => {

    it('construction always logs the starting of data collection', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const razorOutputChannel = api.getRazorOutputChannel();
        const eventEmitterFactory = new TestEventEmitterFactory();
        const logger = new RazorLogger(api, eventEmitterFactory, Trace.Off);
        const eventEmitter = eventEmitterFactory.create<vscode.TextDocument>();

        // Act
        const dataCollector = new ReportIssueDataCollector(eventEmitter.event, logger);

        // Assert
        const lastLog = razorOutputChannel[razorOutputChannel.length - 1];
        assert.ok(lastLog.indexOf('Starting') > 0);
    });

    it('stop always logs the stopping of data collection', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const razorOutputChannel = api.getRazorOutputChannel();
        const eventEmitterFactory = new TestEventEmitterFactory();
        const logger = new RazorLogger(api, eventEmitterFactory, Trace.Off);
        const eventEmitter = eventEmitterFactory.create<vscode.TextDocument>();
        const dataCollector = new ReportIssueDataCollector(eventEmitter.event, logger);

        // Act
        dataCollector.stop();

        // Assert
        const lastLog = razorOutputChannel[razorOutputChannel.length - 1];
        assert.ok(lastLog.indexOf('Stopping') > 0);
    });

    it('construction->stop->collect captures the last focused Razor document', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const eventEmitterFactory = new TestEventEmitterFactory();
        const logger = new RazorLogger(api, eventEmitterFactory, Trace.Off);
        const eventEmitter = eventEmitterFactory.create<vscode.TextDocument>();
        const firstTextDocument = new TestTextDocument('empty', api.Uri.parse('C:/something.cshtml'));
        const expectedTextDocument = new TestTextDocument('empty', api.Uri.parse('C:/something2.cshtml'));

        // Act
        const dataCollector = new ReportIssueDataCollector(eventEmitter.event, logger);
        eventEmitter.fire(firstTextDocument);
        eventEmitter.fire(expectedTextDocument);
        dataCollector.stop();
        const collectionResult = dataCollector.collect();

        // Assert
        assert.equal(collectionResult.document, expectedTextDocument);
    });

    it('construction->stop->collect captures logs between construction and stop', async () => {
        // Arrange
        const api = createTestVSCodeApi();
        const eventEmitterFactory = new TestEventEmitterFactory();
        const logger = new RazorLogger(api, eventEmitterFactory, Trace.Off);
        const eventEmitter = eventEmitterFactory.create<vscode.TextDocument>();
        const expectedLogContent = 'Expected Log Content';
        const unexpectedLogContent = 'SHOULDNOTEXIST';

        // Act
        const dataCollector = new ReportIssueDataCollector(eventEmitter.event, logger);
        logger.logAlways(expectedLogContent);
        dataCollector.stop();
        logger.logAlways(unexpectedLogContent);
        const collectionResult = dataCollector.collect();

        // Assert
        assert.ok(collectionResult.logOutput.indexOf(expectedLogContent) >= 0);
        assert.ok(collectionResult.logOutput.indexOf(unexpectedLogContent) === -1);
    });
});
