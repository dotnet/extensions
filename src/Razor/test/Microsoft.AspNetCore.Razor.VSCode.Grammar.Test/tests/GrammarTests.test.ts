/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { RunAddTagHelperDirectiveSuite } from './AddTagHelperDirective';
import { RunAttributeDirectiveSuite } from './AttributeDirective';
import { RunCodeBlockSuite } from './CodeBlock';
import { RunCodeDirectiveSuite } from './CodeDirective';
import { RunExplicitExpressionSuite } from './ExplicitExpressions';
import { RunFunctionsDirectiveSuite } from './FunctionsDirective';
import { RunImplementsDirectiveSuite } from './ImplementsDirective';
import { RunImplicitExpressionSuite } from './ImplicitExpressions';
import { RunInheritsDirectiveSuite } from './InheritsDirective';
import { RunInjectDirectiveSuite } from './InjectDirective';
import { RunLayoutDirectiveSuite } from './LayoutDirective';
import { RunModelDirectiveSuite } from './ModelDirective';
import { RunNamespaceDirectiveSuite } from './NamespaceDirective';
import { RunPageDirectiveSuite } from './PageDirective';
import { RunRazorCommentSuite } from './RazorComment';
import { RunRemoveTagHelperDirectiveSuite } from './RemoveTagHelperDirective';
import { RunSectionDirectiveSuite } from './SectionDirective';
import { RunTagHelperPrefixDirectiveSuite } from './TagHelperPrefixDirective';
import { RunTransitionsSuite } from './Transitions';
import { RunUsingDirectiveSuite } from './UsingDirective';
import { RunUsingStatementSuite } from './UsingStatement';

// We bring together all test suites and wrap them in one here. The reason behind this is that
// modules get reloaded per test suite and the vscode-textmate library doesn't support the way
// that Jest reloads those modules. By wrapping all suites in one we can guaruntee that the
// modules don't get torn down inbetween suites.

describe('Grammar tests', () => {
    RunTransitionsSuite();
    RunExplicitExpressionSuite();
    RunImplicitExpressionSuite();
    RunCodeBlockSuite();
    RunRazorCommentSuite();

    // Directives
    RunCodeDirectiveSuite();
    RunFunctionsDirectiveSuite();
    RunPageDirectiveSuite();
    RunAddTagHelperDirectiveSuite();
    RunRemoveTagHelperDirectiveSuite();
    RunTagHelperPrefixDirectiveSuite();
    RunModelDirectiveSuite();
    RunImplementsDirectiveSuite();
    RunInheritsDirectiveSuite();
    RunNamespaceDirectiveSuite();
    RunInjectDirectiveSuite();
    RunAttributeDirectiveSuite();
    RunSectionDirectiveSuite();
    RunLayoutDirectiveSuite();
    RunUsingDirectiveSuite();

    // Razor C# Control Structures
    RunUsingStatementSuite();
});
