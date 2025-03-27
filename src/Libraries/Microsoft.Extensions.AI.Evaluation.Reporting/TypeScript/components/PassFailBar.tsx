// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from "@fluentui/react-components";

const usePassFailStyles = makeStyles({
    passFailBadge: {
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        padding: '0 0.25rem',
    },
    passFailBadgeWithBackground: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        padding: '0 0.25rem',
        borderRadius: '4px',
        backgroundColor: tokens.colorNeutralBackground3,
    },
    score: { fontSize: tokens.fontSizeBase200 },
    passFail: {
        display: 'inline-flex',
        flexDirection: 'row',
    },
    passFailVertical: {
        display: 'inline-flex',
        flexDirection: 'column',
    },
    pass: {
        borderTopRightRadius: '2px',
        borderBottomRightRadius: '2px',
        backgroundColor: tokens.colorStatusSuccessBackground3,
        border: `1px solid`,
        borderLeft: 'none',
    },
    fail: {
        borderTopLeftRadius: '2px',
        borderBottomLeftRadius: '2px',
        backgroundColor: tokens.colorStatusDangerBackground3,
        border: `1px solid`,
        borderRight: 'none',
    },
    passVertical: {
        borderBottomLeftRadius: '2px',
        borderBottomRightRadius: '2px',
        backgroundColor: tokens.colorStatusSuccessBackground3,
        border: `1px solid`,
        borderTop: 'none',
    },
    failVertical: {
        borderTopLeftRadius: '2px',
        borderTopRightRadius: '2px',
        backgroundColor: tokens.colorStatusDangerBackground3,
        border: `1px solid`,
        borderBottom: 'none',
    },
    positiveText: {
        color: tokens.colorStatusSuccessForeground3,
    },
    negativeText: {
        color: tokens.colorStatusDangerForeground3,
    },
    allPass: {
        borderRadius: '2px',
        backgroundColor: tokens.colorStatusSuccessBackground3,
        border: `1px solid`,
    },
    allFail: {
        borderRadius: '2px',
        backgroundColor: tokens.colorStatusDangerBackground3,
        border: `1px solid`,
    }
});

export const PassFailBar = ({ pass, total, width, height, onClick }: 
    { pass: number, total: number, width?: string, height?: string, selected: boolean, onClick?: React.MouseEventHandler<HTMLDivElement> }) => {

    const classes = usePassFailStyles();
    const passPct = total > 0 ? (pass / total) * 100 : 0;
    const failPct = 100 - passPct;
    width = width || '3rem';

    if (pass === 0) {
        return <div className={classes.passFail} style={{ width, height }} onClick={onClick}>
            <div className={classes.allFail} style={{ width: `100%` }} >&nbsp;</div>
        </div>;
    } else if (pass === total) {
        return <div className={classes.passFail} style={{ width, height }} onClick={onClick}>
            <div className={classes.allPass} style={{ width: `100%` }} >&nbsp;</div>
        </div>;
    } else {
        return <div className={classes.passFail} style={{ width, height }} onClick={onClick}>
            <div className={classes.fail} style={{ width: `${failPct}%` }} >&nbsp;</div>
            <div className={classes.pass} style={{ width: `${passPct}%` }} >&nbsp;</div>
        </div>;
    }
};

export const PassFailVerticalBar = ({ pass, total, width, height }: { pass: number, total: number, width?: string, height?: string }) => {
    const classes = usePassFailStyles();
    const passPct = total > 0 ? (pass / total) * 100 : 0;
    const failPct = 100 - passPct;
    width = width || '3rem';

    if (pass === 0) {
        return <div className={classes.passFailVertical} style={{ width, height }}>
            <div className={classes.allFail} style={{ height: `100%` }} >&nbsp;</div>
        </div>;
    } else if (pass === total) {
        return <div className={classes.passFailVertical} style={{ width, height }}>
            <div className={classes.allPass} style={{ height: `100%` }} >&nbsp;</div>
        </div>;
    } else {
        return <div className={classes.passFailVertical} style={{ width, height }}>
            <div className={classes.failVertical} style={{ height: `${failPct}%` }} >&nbsp;</div>
            <div className={classes.passVertical} style={{ height: `${passPct}%` }} >&nbsp;</div>
        </div>;
    }
};

export const PassFailBadge = ({ pass, total }: { pass: number; total: number }) => {
    const classes = usePassFailStyles();

    return (<div className={classes.passFailBadgeWithBackground}>
        <span className={classes.score}>{pass}/{total} [{((pass * 100) / total).toFixed(1)}%]</span>
    </div>);
};

export const PassFailBarLabel = ({ pass, total, prevPass, prevTotal }: { pass: number; total: number, prevPass?: number, prevTotal?: number }) => {
    const classes = usePassFailStyles();

    const pct = total === 0 ? undefined : (pass * 100) / total;
    const prevPct = !!prevPass && !!prevTotal ? (prevPass * 100) / prevTotal : undefined;
    const diff = !!prevPct && !!pct ? (pct - prevPct) : undefined;
    const diffText = !!diff ? (diff > 0 ? `+${diff.toFixed(1)}%` : `${diff.toFixed(1)}%`) : <span>&nbsp;</span>;

    return (<div className={classes.passFailBadge}>
        <div className={classes.score}>{pass}/{total}</div>
        <div className={classes.score}>{pct !== undefined && <span>[{pct.toFixed(1)}%]</span>}</div>
        <div className={classes.score}>{<span className={diff && diff > 0 ? classes.positiveText : classes.negativeText}>{diffText}</span>}</div>
    </div>);
};
