// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from "@fluentui/react-components";

export const useStyles = makeStyles({
    headerContainer: {
        display: 'flex', 
        alignItems: 'center', 
        flexDirection: 'row', 
        gap: '0.5rem',
    },
    selectedText: {
        fontWeight: '500',
    },
    scenarioLabel: {
        whiteSpace: 'nowrap',
        fontSize: tokens.fontSizeBase300,
        display: 'flex',
        gap: '0.5rem',
        alignItems: 'center',
    },
    separator: {
        color: tokens.colorNeutralForeground4,
        fontSize: tokens.fontSizeBase200,
        fontWeight: '300',
        padding: '0 0.125rem',
    },
    iterationArea: {
        marginTop: '1rem',
        marginBottom: '1rem',
        maxWidth: '75rem',
    },
   dismissableSectionHeader: {
        display: 'flex',
        alignItems: 'center',
    },
    sectionSubHeader: {
        fontSize: tokens.fontSizeBase300,
        fontWeight: '500',
        marginBottom: '0.25rem',
    },
    sectionContent: {
        marginBottom: '0.75rem',
    },
    failMessage: {
        color: tokens.colorStatusDangerForeground2,
        marginBottom: '0.25rem',
    },
    warningMessage: {
        color: tokens.colorStatusWarningForeground2,
        marginBottom: '0.25rem',
    },
    infoMessage: {
        color: tokens.colorNeutralForeground1,
        marginBottom: '0.25rem',
    },
    cacheHitIcon: {
        color: tokens.colorPaletteGreenForeground1,
    },
    cacheMissIcon: {
        color: tokens.colorPaletteRedForeground1,
    },
    cacheHit: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
        color: tokens.colorPaletteGreenForeground1,
    },
    cacheMiss: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
        color: tokens.colorPaletteRedForeground1,
    },
    cacheKeyCell: {
        maxWidth: '240px',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    cacheKey: {
        fontFamily: tokens.fontFamilyMonospace,
        fontSize: '0.7rem',
        padding: '0.1rem 0.3rem',
        backgroundColor: tokens.colorNeutralBackground3,
        borderRadius: '4px',
        display: 'block',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
    },
    noCacheKey: {
        color: tokens.colorNeutralForeground3,
        fontStyle: 'italic',
    },
    tableContainer: {
        overflowX: 'auto',
        maxWidth: '75rem',
    },
    cacheKeyContainer: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
    },
    autoWidthTable: {
        tableLayout: 'auto',
        width: '100%',
    },
    tableHeaderCell: {
        fontWeight: '600',
        fontSize: tokens.fontSizeBase300,
        borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    },
    tablesContainer: {
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
    },
    tableWrapper: {
        flex: '1',
    },
    copyButton: {
        background: 'none',
        border: 'none',
        cursor: 'pointer',
        padding: '2px',
        color: tokens.colorNeutralForeground3,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: '3px',
        '&:hover': {
            backgroundColor: tokens.colorNeutralBackground4,
            color: tokens.colorNeutralForeground1,
        }
    },
    executionHeaderCell: {
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100%',
        width: '100%',
        padding: '0.5rem',
    },
    currentExecutionBackground: {
        backgroundColor: tokens.colorNeutralBackground4,
    },
    currentExecutionForeground: {
        fontWeight: '600',
    },
    verticalText: {
        writingMode: 'sideways-rl',
        transform: 'rotate(2000deg)',
        fontSize: tokens.fontSizeBase200,
        fontWeight: '400',
        color: tokens.colorNeutralForeground2,
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
    },
    historyMetricCell: {
        fontSize: tokens.fontSizeBase200,
        fontWeight: '400',
        color: tokens.colorNeutralForeground2,
    },
    scenarioHistoryCell: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100%',
        width: '100%',
        padding: '0.5rem',
        gap: '0.25rem',
    },
    diagnosticErrorCell: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
        color: tokens.colorStatusDangerForeground2,
        whiteSpace: 'nowrap',
    },
    diagnosticWarningCell: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
        color: tokens.colorStatusWarningForeground2,
        whiteSpace: 'nowrap',
    },
    diagnosticInfoCell: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
        color: tokens.colorNeutralForeground1,
        whiteSpace: 'nowrap',
    },
    diagnosticMessageText: {
        fontFamily: tokens.fontFamilyBase,
        whiteSpace: 'pre-wrap',
        overflow: 'auto',
        margin: 0,
        padding: 0,
        display: 'block',
    },
    diagnosticSeverityCell: {
        width: '1%',
        height: 'auto',
        whiteSpace: 'nowrap',
        verticalAlign: 'top',
        padding: '1em',
    },
    diagnosticMessageCell: {
        width: '100%',
        height: 'auto',
        verticalAlign: 'top',
        padding: '1em',
    },
    diagnosticCopyButtonCell: {
        width: '1%',
        height: 'auto',
        whiteSpace: 'nowrap',
        verticalAlign: 'top',
        padding: '1em',
    },
});
