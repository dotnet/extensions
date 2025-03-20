// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useState } from 'react';
import { Settings28Regular } from '@fluentui/react-icons';
import { Drawer, DrawerBody, DrawerHeader, DrawerHeaderTitle, Switch } from '@fluentui/react-components';
import { makeStyles } from '@fluentui/react-components';
import './App.css';
import { SummaryResults } from './Summary';
import { ScenarioGroup } from './ScenarioTree';

type AppProperties = {
  dataset: Dataset,
  tree: SummaryResults,
};

const useStyles = makeStyles({
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', position: 'sticky', top: 0, backgroundColor: 'white', zIndex: 1 },
  footerText: { fontSize: '0.8rem', marginTop: '2rem' },
  closeButton: { position: 'absolute', top: '1.5rem', right: '1rem', cursor: 'pointer', fontSize: '2rem' },
  switchLabel: { fontSize: '1rem', paddingTop: '1rem' },
  drawerBody: { paddingTop: '1rem' },
});

function App({ dataset, tree }: AppProperties) {
  const classes = useStyles();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const [renderMarkdown, setRenderMarkdown] = useState(true);

  const toggleSettings = () => setIsSettingsOpen(!isSettingsOpen);
  const toggleRenderMarkdown = () => setRenderMarkdown(!renderMarkdown);
  const closeSettings = () => setIsSettingsOpen(false);

  return (
    <>
      <div className={classes.header}>
        <h1>AI Evaluation Report</h1>
        <Settings28Regular onClick={toggleSettings} style={{ cursor: 'pointer' }} />
      </div>

      <ScenarioGroup node={tree.primaryResult} renderMarkdown={renderMarkdown} />

      <p className={classes.footerText}>Generated at {dataset.createdAt} by Microsoft.Extensions.AI.Evaluation.Reporting version {dataset.generatorVersion}</p>

      <Drawer open={isSettingsOpen} onOpenChange={toggleSettings} position='end'>
        <DrawerHeader>
          <DrawerHeaderTitle>Settings</DrawerHeaderTitle>
          <span className={classes.closeButton} onClick={closeSettings}>&times;</span>
        </DrawerHeader>
        <DrawerBody className={classes.drawerBody}>
          <Switch checked={renderMarkdown} onChange={toggleRenderMarkdown} label={<span className={classes.switchLabel}>Render markdown for conversations</span>} />
        </DrawerBody>
      </Drawer>
    </>
  );
}

export default App;
