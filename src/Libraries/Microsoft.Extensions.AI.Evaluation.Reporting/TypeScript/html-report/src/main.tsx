// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from '../../components/App.tsx'
import { FluentProvider, webLightTheme } from '@fluentui/react-components';
import { createScoreSummary } from '../../components/Summary.ts';
import { ReportContextProvider } from '../../components/ReportContext.tsx';

let dataset: Dataset = { scenarioRunResults: [] };

if (!import.meta.env.PROD) {
  // This only runs in development. In production the data is embedded into the dataset variable declaration above.
  // run `node init-devdata.js` to populate the data file from the most recent execution.
  const imported = await import("../devdata.json");
  dataset = imported.default as unknown as Dataset;
}

const scoreSummary = createScoreSummary(dataset);

createRoot(document.getElementById('root')!).render(
  <FluentProvider theme={webLightTheme}>
    <StrictMode>
      <ReportContextProvider dataset={dataset} scoreSummary={scoreSummary}>
        <App />
      </ReportContextProvider>
    </StrictMode>
  </FluentProvider>,
)
