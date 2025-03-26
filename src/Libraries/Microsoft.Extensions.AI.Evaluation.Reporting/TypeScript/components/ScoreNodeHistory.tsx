import { makeStyles, tokens } from "@fluentui/react-components";

const useStyles = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
        padding: '1rem',
        borderRadius: '4px',
        width: '100%',
        backgroundColor: tokens.colorNeutralBackground1,
    },
    title: {
        fontSize: '1.25rem',
        fontWeight: 'bold',
        marginBottom: '0.5rem',
    },
    content: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.5rem',
    },
});

export const ScoreNodeHistory = () => {
    const classes = useStyles();

    return (<div className={classes.root}>
        <div className={classes.title}>Score Node History</div>
        <div className={classes.content}>
            {/* Add your content here */}
            <p>This is where the score node history will be displayed.</p>
        </div>
    </div>);
};