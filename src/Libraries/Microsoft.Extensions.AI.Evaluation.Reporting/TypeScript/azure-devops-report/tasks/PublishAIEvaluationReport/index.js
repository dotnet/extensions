// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import {getPathInput, getBoolInput, addAttachment} from 'azure-pipelines-task-lib';

const disableAttachmentUpload = getBoolInput('disableAttachmentUpload', false);

if (disableAttachmentUpload) {
    console.log('Attachment upload is disabled; Upload an attachment with the type \'ai-eval-report-json\' and name \'ai-eval-report\'');
} else {
    const path = getPathInput('reportFile', true, true);
    if (!path) {
        throw new Error('reportFile input is required');
    }
    console.log(`Using reportFile: ${path}`);

    addAttachment('ai-eval-report-json', 'ai-eval-report', path);
    
    console.log(`Published reportFile: ${path}`);
}

