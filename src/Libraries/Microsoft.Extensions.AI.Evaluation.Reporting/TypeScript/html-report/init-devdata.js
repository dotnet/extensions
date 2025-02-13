// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import fs from 'fs/promises';
import path from 'path';
import {fileURLToPath} from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const resultsDir = path.join(__dirname, '../../../.storage/results');
const scenarioDirents = await fs.readdir(resultsDir, { withFileTypes: true });

let maxBirthtime = 0;
let pickedDir = null;
for (const dirent of scenarioDirents) {
    if (!dirent.isDirectory()) {
        continue;
    }
    const stat = await fs.stat(path.join(dirent.parentPath, dirent.name));
    if (stat.birthtimeMs > maxBirthtime) {
        maxBirthtime = stat.birthtimeMs;
        pickedDir = dirent;
    }
}

if (!pickedDir) {
    throw new Error('No scenarios found');
}

console.log(`Using execution ${pickedDir.name}`);

const scenarioRunResults = [];
const groups = await fs.readdir(path.join(pickedDir.parentPath, pickedDir.name), { withFileTypes: true });
for (const g of groups) {
    if (!g.isDirectory()) {
        continue;
    }
    const files = await fs.readdir(path.join(g.parentPath, g.name), { withFileTypes: true });
    for (const f of files) {
        if (f.isFile() && f.name.endsWith('.json')) {
            const fullpath = path.join(f.parentPath, f.name)
            console.log(fullpath);
            const contents = await fs.readFile(fullpath);
            scenarioRunResults.push(JSON.parse(contents));
        };
    }
}
const dataset = { scenarioRunResults, createdAt: new Date().toISOString(), generatorVersion: '0.0.42-devdata' };

await fs.writeFile(path.join(__dirname, 'devdata.json'), JSON.stringify(dataset, null, 2));
