# Stage 7 - Confirm the Support-Page Update

The [.NET Platform Extensions support policy page](https://dotnet.microsoft.com/en-us/platform/support/policy/extensions) lists the supported packages and their current versions. It is maintained by a **partner team**, so this stage is a **review**, not an edit: confirm their update lands and is complete.

## Steps

1. **Find the pull request** into [`dotnet/website`](https://github.com/dotnet/website) that updates the extensions support page for this release (it edits `website/src/netlandingpage/Pages/platform/support/policy/extensions.cshtml`). Search open and recently-merged PRs.
2. **Review it** for this release:
   - The supported version and its release date are updated to this release.
   - The previous minor version has moved to the out-of-support table with the correct end-of-support date.
3. **Check the package list** against what shipped. Compare the page's listed packages to the packages published in this release, and flag any that are missing -- in particular:
   - **Newly published** packages (new in this release).
   - **Recently-stabilized** packages (previously preview, now stable as of this release).

   Exclude preview-only packages, which are not listed.
4. If the pull request is missing or incomplete, raise it with the partner team.

## After the stage

This is the final validation step. The release is complete once its symbols are public (Stage 5), the branches are reconciled (Stage 6), and the support-page listing is confirmed.
