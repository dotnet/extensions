# Validate Release

Confirms a published `dotnet/extensions` release is correct and finalizes it: verify that the published symbols are on the Microsoft symbol server (msdl) with working Source Link, then reconcile the internal, public, and `main` branches.

Run this playbook after **publish-release**.

- Stage 5 is automated symbol verification.
- Stage 6 stages reconciliation merges when needed (pushing and PR completion are left to the user).
- Stage 7 handles support-page follow-up based on release type and package novelty.

For servicing releases prepared directly on public `release/<major>.<minor>`, Stage 6 is often unnecessary because commits were backported from `main` into the release branch up front. In that case, run Stage 5 and run Stage 6 only if the user explicitly asks for additional branch-flow follow-up.

## Stage 5 - Verify Source Link and Symbols

Run the Source Link sweep against the published packages until every library package reports `valid` on msdl. This is the **release sign-off gate** -- a persistent `symbols-not-indexed` result means the official release build never reached the public `.NET <major>` channel (see publish-release, Stage 4).

Read and follow [references/stage-5-verify-source-link.md](references/stage-5-verify-source-link.md).

## Stage 6 - Reconcile Branches

Merge the internal release branch back out to the public `release/<major>.<minor>` branch (discarding the internal-only infrastructure changes), then merge that public branch into `main` (reverting the stable-versioning flip). Each merge lands as a merge commit (never squashed); the agent stages the merges and shows the diff, but pushing and completing the merge-commit PRs (which need elevation) are user-directed.

Read and follow [references/stage-6-reconcile-branches.md](references/stage-6-reconcile-branches.md).

## Stage 7 - Confirm the Support-Page Update

Stage 7 is conditional:

- **Monthly releases**: the support page is maintained by a partner team; review their PR and confirm completeness.
- **Servicing releases that only service existing packages**: Stage 7 can be skipped.
- **Servicing releases that introduce newly shipped packages**: the release manager must author and submit the `dotnet/website` PR that adds those packages to the support page.

Read and follow [references/stage-7-support-page.md](references/stage-7-support-page.md).

## Done

These three final stages complete the branch-level release and its verification. Tagging and publishing the GitHub release notes are the **write-release-notes** playbook.
