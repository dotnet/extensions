/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { RunAddTagHelperDirectiveSuite } from './AddTagHelperDirective';
import { RunCodeBlockSuite } from './CodeBlock';
import { RunCodeDirectiveSuite } from './CodeDirective';
import { RunExplicitExpressionSuite } from './ExplicitExpressions';
import { RunFunctionsDirectiveSuite } from './FunctionsDirective';
import { RunImplicitExpressionSuite } from './ImplicitExpressions';
import { RunModelDirectiveSuite } from './ModelDirective';
import { RunPageDirectiveSuite } from './PageDirective';
import { RunRemoveTagHelperDirectiveSuite } from './RemoveTagHelperDirective';
import { RunTagHelperPrefixDirectiveSuite } from './TagHelperPrefixDirective';
import { RunTransitionsSuite } from './Transitions';

// We bring together all test suites and wrap them in one here. The reason behind this is that
// modules get reloaded per test suite and the vscode-textmate library doesn't support the way
// that Jest reloads those modules. By wrapping all suites in one we can guaruntee that the
// modules don't get torn down inbetween suites.

describe('Grammar tests', () => {
    RunTransitionsSuite();
    RunExplicitExpressionSuite();
    RunImplicitExpressionSuite();
    RunCodeBlockSuite();

    // Directives
    RunCodeDirectiveSuite();
    RunFunctionsDirectiveSuite();
    RunPageDirectiveSuite();
    RunAddTagHelperDirectiveSuite();
    RunRemoveTagHelperDirectiveSuite();
    RunTagHelperPrefixDirectiveSuite();
    RunModelDirectiveSuite();
});
