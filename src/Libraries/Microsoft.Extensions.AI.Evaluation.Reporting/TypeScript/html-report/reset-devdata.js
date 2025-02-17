// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import fs from 'fs/promises';
import path from 'path';
import {fileURLToPath} from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const dataset = { scenarioRunResults: [], createdAt: new Date().toISOString(), generatorVersion: '0.0.1' };
await fs.writeFile(path.join(__dirname, 'devdata.json'), JSON.stringify(dataset, null, 2));
