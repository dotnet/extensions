// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { glob } from 'glob';
import { exec, execSync } from 'child_process';
import { fileURLToPath } from 'url';
import { dirname } from 'path';
import { existsSync } from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

// Get the newest file in the dir
const newestFile = async (pattern) => {
    let files = await glob(pattern, { stat: true, withFileTypes: true });
    let mtime = 0;
    let newestFile;
    for (const file of files) {
        if (file.isFile() && file.mtimeMs > mtime) {
            mtime = file.mtimeMs;
            newestFile = file;
        }
    }
    return newestFile;
};

let newestReportFile = await newestFile(__dirname + "/html-report/**");
let newestComponentFile = await newestFile(__dirname + "/components/**");

let newestDistFile = await newestFile(__dirname + "/html-report/dist/**");

if (!newestDistFile || newestDistFile.mtimeMs < Math.max(newestReportFile.mtimeMs, newestComponentFile.mtimeMs)) {
    process.chdir(__dirname);

    // Make sure the devdata.json is populated
    if (!existsSync(__dirname + "/html-report/devdata.json")) {
        execSync("node " + __dirname + "/html-report/reset-devdata.js");
    }

    // Run the build
    exec("npm run build", (err, stdout, stderr) => {
        if (err) {
            console.error(err);
            return;
        }
        console.log(stdout);
    });
} else {
    console.log("Build is up to date");
}
