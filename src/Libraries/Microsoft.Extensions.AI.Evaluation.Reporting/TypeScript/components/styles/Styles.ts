// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from "@fluentui/react-components";

export const useStyles = makeStyles({
    tableContainer: {
        overflowX: 'auto',
        maxWidth: '75rem',
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
