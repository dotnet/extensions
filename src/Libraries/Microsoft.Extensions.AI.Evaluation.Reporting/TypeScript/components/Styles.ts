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
    hint: {
        fontFamily: tokens.fontFamilyMonospace,
        opacity: 0.6,
        fontSize: '0.7rem',
        paddingTop: '0.25rem',
        paddingLeft: '1rem',
        whiteSpace: 'nowrap',
        display: 'flex',
        alignItems: 'center',
        gap: '0.25rem',
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
    },
    section: {
        marginTop: '0.75rem',
        marginBottom: '0.75rem',
        padding: '1rem',
        border: '2px solid ' + tokens.colorNeutralStroke1,
        borderRadius: '8px',
        right: '0',
    },
    sectionHeader: {
        display: 'flex',
        alignItems: 'center',
        cursor: 'pointer',
        userSelect: 'none',
    },
    sectionHeaderText: {
        margin: 0,
        marginLeft: '0.5rem',
        fontSize: tokens.fontSizeBase300,
        fontWeight: '500',
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
    sectionContainer: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
        padding: '0.75rem 0',
        cursor: 'text',
        position: 'relative',
        maxWidth: '75rem',
        '& pre': {
            whiteSpace: 'pre-wrap',
            wordWrap: 'break-word',
        },
    },
    messageRow: {
        display: 'flex',
        flexDirection: 'column',
        position: 'relative',
    },
    userMessageRow: {
        marginLeft: '0',
        marginRight: '10rem',
    },
    assistantMessageRow: {
        marginLeft: '10rem',
        marginRight: '0',
    },
    messageParticipantName: {
        fontSize: tokens.fontSizeBase200,
        marginBottom: '0.25rem',
        color: tokens.colorNeutralForeground3,
        paddingLeft: '0.5rem',
    },
    messageBubble: {
        padding: '0.75rem 1rem',
        borderRadius: '12px',
        overflow: 'hidden',
        wordBreak: 'break-word',
        backgroundColor: tokens.colorNeutralBackground3,
        border: '1px solid ' + tokens.colorNeutralStroke2,
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
    preWrap: {
        whiteSpace: 'pre-wrap',
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
});