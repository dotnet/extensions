// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens, Text } from '@fluentui/react-components';

const useStyles = makeStyles({
    container: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
        padding: '2rem',
        borderRadius: tokens.borderRadiusXLarge,
        border: `1px dashed ${tokens.colorNeutralStroke2}`,
        backgroundColor: tokens.colorNeutralBackground2,
        color: tokens.colorNeutralForeground3,
    },
});

// Minimal titled container used by the not-yet-implemented views in P0. Renders
// without throwing; P1/P3 replace each consumer with its real view.
export const PlaceholderView = ({ title, message }: { title: string; message: string }) => {
    const classes = useStyles();
    return (
        <div className={classes.container} data-screen-label={title.toLowerCase()}>
            <Text as="h2" weight="semibold" size={500}>{title}</Text>
            <Text>{message}</Text>
        </div>
    );
};
