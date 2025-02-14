// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from "@fluentui/react-components";

const useStyles = makeStyles({
    passFail: {
        display: 'inline-flex',
        flexDirection: 'row',
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

export const PassFailBar = ({ pass, total, width, height }: { pass: number, total: number, width?: string, height?: string }) => {
    const classes = useStyles();
    const passPct = total > 0 ? (pass / total) * 100 : 0;
    const failPct = 100 - passPct;
    width = width || '3rem';

    if (pass === 0) {
        return <div className={classes.passFail} style={{ width, height }}>
            <div className={classes.allFail} style={{ width: `100%` }} >&nbsp;</div>
        </div>;
    } else if (pass === total) {
        return <div className={classes.passFail} style={{ width, height }}>
            <div className={classes.allPass} style={{ width: `100%` }} >&nbsp;</div>
        </div>;
    } else {
        return <div className={classes.passFail} style={{ width, height }}>
            <div className={classes.fail} style={{ width: `${failPct}%` }} >&nbsp;</div>
            <div className={classes.pass} style={{ width: `${passPct}%` }} >&nbsp;</div>
        </div>;
    }
};
