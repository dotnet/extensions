// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

if (!process.env.AGENT_TEMPDIRECTORY) {
    console.error('AGENT_TEMPDIRECTORY not set. This script is intended to be run in an Azure DevOps pipeline.');
    process.exit(1);    
}
console.log('what',process.env.AGENT_TEMPDIRECTORY);


import Sign from "microbuild-signing";
import { glob } from "glob";

const searchPath = process.argv[2]
if (!searchPath) {
    console.error('search path not provided');
    process.exit(1);
}

try {
    const files = await glob('**/*.vsix', { cwd: searchPath, absolute: true });

    for (const file of files) {
        console.log(`Signing file: ${file}`);
    }
    console.log(files);

    const result = Sign({
        'VsixSHA2': files,
    });

    process.exitCode = result;

} catch (err) {
    console.error(err);
}
