# Validate Release

Confirms a published `dotnet/extensions` release is correct and finalizes it: verify that the shipped symbols are on the Microsoft symbol server (msdl) with working Source Link, then reconcile the internal, public, and `main` branches.

Run this playbook after **publish-release**. Stage 1 is automated symbol verification; Stage 2 stages the reconciliation merges (pushing and PR completion are left to the user); Stage 3 confirms the support-page listing.

## Stage 1 - Verify Source Link and Symbols

Run the Source Link sweep against the published packages until every library package reports `valid` on msdl. This is the **release sign-off gate** -- a persistent `symbols-not-indexed` result means the shipping build never reached the public `.NET <major>` channel (see publish-release, Stage 2).

Read and follow [references/stage-1-verify-source-link.md](references/stage-1-verify-source-link.md).

## Stage 2 - Reconcile Branches

Merge the internal release branch back out to the public `release/<major>.<minor>` branch (discarding the internal-only infrastructure changes), then merge that public branch into `main` (reverting the stable-versioning flip). Each merge lands as a merge commit (never squashed); the agent stages the merges and shows the diff, but pushing and completing the merge-commit PRs (which need elevation) are user-directed.

Read and follow [references/stage-2-reconcile-branches.md](references/stage-2-reconcile-branches.md).

## Stage 3 - Confirm the Support-Page Update

The .NET Platform Extensions support page is maintained by a partner team. Confirm that a pull request updating it has been opened for this release, review it, and flag any newly published or recently-stabilized packages that are missing from the list.

Read and follow [references/stage-3-support-page.md](references/stage-3-support-page.md).

## Done

The three stages complete the branch-level release and its verification. Tagging and publishing the GitHub release notes are the **write-release-notes** playbook.
