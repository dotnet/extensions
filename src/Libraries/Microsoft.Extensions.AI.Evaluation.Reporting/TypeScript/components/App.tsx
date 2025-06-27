// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useState } from 'react';
import { Settings28Regular, FilterDismissRegular, DismissRegular, ArrowDownloadRegular } from '@fluentui/react-icons';
import { Button, Drawer, DrawerBody, DrawerHeader, DrawerHeaderTitle, SearchBox, Switch, Tooltip } from '@fluentui/react-components';
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
    padding: '0rem 2rem 1rem 2rem',
    backgroundColor: tokens.colorNeutralBackground1,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    marginBottom: '1rem',
  },
  headerTop: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  body: {
    padding: '0rem 2rem',
  },
  headerActions: {
    display: 'flex',
    alignItems: 'center',
    gap: '12px',
  },
  footerText: { fontSize: '0.8rem', marginTop: '2rem' },
  closeButton: {
    position: 'absolute',
    top: '1.5rem',
    right: '1rem',
  },
  switchLabel: { fontSize: '1rem', paddingTop: '1rem' },
  drawerBody: { paddingTop: '1rem' },
});

export const App = () => {
  const classes = useStyles();
  const { dataset, scoreSummary, selectedTags, clearFilters, searchValue, setSearchValue } = useReportContext();
  const [isSettingsOpen, setIsSettingsOpen] = useState(false);
  const { renderMarkdown, setRenderMarkdown, prettifyJson, setPrettifyJson } = useReportContext();
  const { globalTags, filterableTags } = categorizeAndSortTags(dataset, scoreSummary.primaryResult.executionName);

  const toggleSettings = () => setIsSettingsOpen(!isSettingsOpen);
  const closeSettings = () => setIsSettingsOpen(false);

  const downloadDataset = () => {
    // create a stringified JSON of the dataset
    const dataStr = JSON.stringify(dataset, null, 2);

    // create a link to download the JSON file in the page and click it
    const blob = new Blob([dataStr], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${scoreSummary.primaryResult.executionName}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <div className={classes.header}>
        <div className={classes.headerTop}>
          <h1>AI Evaluation Report</h1>
          <div className={classes.headerActions}>
            {(selectedTags.length > 0 || !!searchValue) && (
              <Tooltip content="Clear Filters" relationship="description">
                <Button icon={<FilterDismissRegular />} appearance="subtle" onClick={clearFilters} />
              </Tooltip>
            )}
            <SearchBox placeholder="Search / Filter " value={searchValue} type="text"
              style={{ width: "16rem" }}
              onChange={(_ev, data) => setSearchValue(data.value)} />
            <Tooltip content="Download Data as JSON" relationship="description">
              <Button icon={<ArrowDownloadRegular />} appearance="subtle" onClick={downloadDataset} aria-label="Download Data as JSON" />
            </Tooltip>
            <Tooltip content="Settings" relationship="description">
              <Button icon={<Settings28Regular />} appearance="subtle" onClick={toggleSettings} aria-label="Settings" />
            </Tooltip>
          </div>
        </div>
        <GlobalTagsDisplay globalTags={globalTags} />

        <FilterableTagsDisplay
          filterableTags={filterableTags}
        />

        <ScoreNodeHistory />
      </div>

      <div className={classes.body}>
        <ScenarioGroup
          node={scoreSummary.primaryResult}
          scoreSummary={scoreSummary}
        />
      </div>

      <p className={classes.footerText}>
        Generated at {dataset.createdAt} by Microsoft.Extensions.AI.Evaluation.Reporting version {dataset.generatorVersion}
      </p>

      <Drawer open={isSettingsOpen} onOpenChange={toggleSettings} position="end">
        <DrawerHeader>
          <DrawerHeaderTitle>Settings</DrawerHeaderTitle>
          <Button className={classes.closeButton} icon={<DismissRegular />} appearance="subtle" onClick={closeSettings} />
        </DrawerHeader>
        <DrawerBody className={classes.drawerBody}>
          <Switch
            checked={renderMarkdown}
            onChange={(_ev, data) => setRenderMarkdown(data.checked)}
            label={<span className={classes.switchLabel}>Render markdown for conversations</span>}
          />
          <Switch
            checked={prettifyJson}
            onChange={(_ev, data) => setPrettifyJson(data.checked)}
            label={<span className={classes.switchLabel}>Pretty print JSON content</span>}
          />
        </DrawerBody>
      </Drawer>
    </>
  );
};
