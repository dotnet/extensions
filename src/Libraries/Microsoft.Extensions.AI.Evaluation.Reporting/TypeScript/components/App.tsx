// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles } from '@fluentui/react-components';
import './App.css';
import { ScoreNode } from './Summary';
import { ScenarioGroup } from './ScenarioTree';

type AppProperties = {
  dataset: Dataset,
  tree: ScoreNode,
};

const useStyles = makeStyles({
  footerText: { fontSize: '0.8rem', marginTop: '2rem' }
})

function App({dataset, tree}:AppProperties) {
  const classes = useStyles();
  return (
    <>
      <h1>AI Evaluation Report</h1>

      <ScenarioGroup node={tree} />

      <p className={classes.footerText}>Generated at {dataset.createdAt} by Microsoft.Extensions.AI.Evaluation.Reporting version {dataset.generatorVersion}</p>
    </>
  )
}

export default App
