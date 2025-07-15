﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useState } from 'react';
import { Settings28Regular, FilterDismissRegular, Dismiss20Regular } from '@fluentui/react-icons';
import { Drawer, DrawerBody, DrawerHeader, DrawerHeaderTitle, Switch, Tooltip } from '@fluentui/react-components';
import { makeStyles } from '@fluentui/react-components';
import './App.css';
import { ScenarioGroup } from './ScenarioTree';
import { GlobalTagsDisplay, FilterableTagsDisplay, categorizeAndSortTags } from './TagsDisplay';
import { tokens } from '@fluentui/react-components';
import { ScoreNodeHistory } from './ScoreNodeHistory';
import { useReportContext } from './ReportContext';

const useStyles = makeStyles({
  header: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    position: 'sticky',
    top: 0,
    zIndex: 1,
    paddingBottom: '12px',
    backgroundColor: tokens.colorNeutralBackground1,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    marginBottom: '1rem',
  },
  headerTop: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  iconButton: {
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    width: '40px',
    height: '40px',
    borderRadius: '6px',
    transition: 'all 0.2s ease-in-out',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground4,
    },
  },
  filterButton: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
    fontSize: '20px',
    width: '40px',
    height: '40px',
    borderRadius: '6px',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground4,
    },
  },
  footerText: { fontSize: '0.8rem', marginTop: '2rem' },
  closeButton: {
    position: 'absolute',
    top: '1.5rem',
    right: '1rem',
    cursor: 'pointer',
    fontSize: '2rem',
    width: '28px',
    height: '28px',
    borderRadius: '6px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    '&:hover': {
      backgroundColor: tokens.colorNeutralBackground4,
    },
  },
  switchLabel: { fontSize: '1rem', paddingTop: '1rem' },
  drawerBody: { paddingTop: '1rem' },
});

function App() {
  const classes = useStyles();
  const { dataset, scoreSummary, selectedTags, clearFilters } = useReportContext();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const { renderMarkdown, setRenderMarkdown } = useReportContext();
  const { globalTags, filterableTags } = categorizeAndSortTags(dataset);

  const toggleSettings = () => setIsSettingsOpen(!isSettingsOpen);
  const closeSettings = () => setIsSettingsOpen(false);

  return (
    <>
      <div className={classes.header}>
        <div className={classes.headerTop}>
          <h1>AI Evaluation Report</h1>
          <div className={classes.headerActions}>
            {selectedTags.length > 0 && (
              <Tooltip content="Clear Filters" relationship="description">
                <div className={`${classes.iconButton} ${classes.filterButton}`} onClick={clearFilters}>
                  <FilterDismissRegular />
                </div>
              </Tooltip>
            )}
            <Tooltip content="Settings" relationship="description">
              <div className={classes.iconButton} onClick={toggleSettings}>
                <Settings28Regular />
              </div>
            </Tooltip>
          </div>
        </div>
        <GlobalTagsDisplay globalTags={globalTags} />

        <FilterableTagsDisplay
          filterableTags={filterableTags}
        />

        <ScoreNodeHistory />
      </div>

      <ScenarioGroup
        node={scoreSummary.primaryResult}
        scoreSummary={scoreSummary}
      />

      <p className={classes.footerText}>
        Generated at {dataset.createdAt} by Microsoft.Extensions.AI.Evaluation.Reporting version {dataset.generatorVersion}
      </p>

      <Drawer open={isSettingsOpen} onOpenChange={toggleSettings} position="end">
        <DrawerHeader>
          <DrawerHeaderTitle>Settings</DrawerHeaderTitle>
          <span className={classes.closeButton} onClick={closeSettings}><Dismiss20Regular /></span>
        </DrawerHeader>
        <DrawerBody className={classes.drawerBody}>
          <Switch
            checked={renderMarkdown}
            onChange={(_ev, data) => setRenderMarkdown(data.checked)}
            label={<span className={classes.switchLabel}>Render markdown for conversations</span>}
          />
        </DrawerBody>
      </Drawer>
    </>
  );
}

export default App;
