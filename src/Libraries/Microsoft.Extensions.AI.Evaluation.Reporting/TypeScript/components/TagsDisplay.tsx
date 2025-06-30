// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from '@fluentui/react-components';
import { useReportContext } from './ReportContext';

const useStyles = makeStyles({
  tagsContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    marginBottom: '8px',
    paddingTop: '6px',
  },
  tagsRow: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '6px',
  },
  tagBubble: {
    padding: '2px 10px',
    borderRadius: '12px',
    backgroundColor: tokens.colorNeutralBackground3,
    fontSize: '0.75rem',
    cursor: 'pointer',
    transition: 'all 0.2s ease-in-out',
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    lineHeight: '1.2',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ':focus': {
      outline: `2px solid ${tokens.colorNeutralForeground3}`,
    },
    ':hover': {
      opacity: 0.9,
      boxShadow: tokens.shadow4,
    },
    '&.selected': {
      boxShadow: tokens.shadow8,
      border: `1px solid ${tokens.colorNeutralForeground3}`,
      backgroundColor: tokens.colorNeutralBackground3Pressed,
    }
  },
  globalTagBubble: {
    backgroundColor: tokens.colorBrandBackground2,
    border: `1px solid ${tokens.colorBrandStroke1}`,
    cursor: 'default',
    ':hover': {
      backgroundColor: tokens.colorBrandBackground2,
      boxShadow: 'none',
      cursor: 'default'
    },
    '&.selected': {
      backgroundColor: tokens.colorBrandBackground2
    }
  },
});

export type TagInfo = { tag: string; count: number };

export function categorizeAndSortTags(dataset: Dataset, primaryExecutionName: string): { 
  globalTags: TagInfo[];
  filterableTags: TagInfo[];
} {
  const tagCounts = new Map<string, number>();
  const primaryResults = dataset.scenarioRunResults.filter(result => result.executionName === primaryExecutionName);
  const totalResults = primaryResults.length;
  
  primaryResults.forEach(result => {
    if (result.tags) {
      result.tags.forEach(tag => {
        const currentCount = tagCounts.get(tag) || 0;
        tagCounts.set(tag, currentCount + 1);
      });
    }
  });
  
  const globalTags: Array<{ tag: string, count: number }> = [];
  const filterableTags: Array<{ tag: string, count: number }> = [];
  
  tagCounts.forEach((count, tag) => {
    const tagInfo = { tag, count };
    if (count === totalResults) {
      globalTags.push(tagInfo);
    } else {
      filterableTags.push(tagInfo);
    }
  });
  
  filterableTags.sort((a, b) => b.count - a.count);
  
  return { globalTags, filterableTags };
}

export function GlobalTagsDisplay({ globalTags }: { globalTags: TagInfo[] }) {
  const classes = useStyles();

  if (globalTags.length === 0) {
    return null;
  }

  return (
    <div className={classes.tagsRow}>
      {globalTags.map(({ tag, count }) => (
        <div 
          key={tag} 
          className={`${classes.tagBubble} ${classes.globalTagBubble}`}
          title={`${tag} (appears on all ${count} results)`}
        >
          {tag}
        </div>
      ))}
    </div>
  );
}

export interface FilterableTagsDisplayProps {
  filterableTags: TagInfo[];
}

export function FilterableTagsDisplay({ filterableTags }: FilterableTagsDisplayProps) {
  const classes = useStyles();
  const {selectedTags, handleTagClick} = useReportContext();

  if (filterableTags.length === 0) {
    return null;
  }

  const isSelected = (tag: string) => selectedTags.includes(tag);

  return (
    <div className={classes.tagsContainer}>
      <div className={classes.tagsRow}>
        {filterableTags.map(({ tag, count }) => (
          <div 
            key={tag} 
            className={`${classes.tagBubble} ${isSelected(tag) ? 'selected' : ''}`}
            title={`${tag} (appears on ${count} results) - Click to filter by this tag`}
            onClick={() => handleTagClick(tag)}
            onKeyDown={(e) => e.key === 'Enter' && handleTagClick(tag)}
            tabIndex={0}
            role="checkbox"
            aria-checked={isSelected(tag)}
            >
            {tag}
          </div>
        ))}
      </div>
    </div>
  );
}
