# Publish Release

Ships a prepared `dotnet/extensions` release: land the prepared branch on `internal/release/<major>.<minor>`, then publish the packages to nuget.org and promote the shipping build to the public channel so its symbols reach the Microsoft symbol server (msdl).

Run this playbook after **prepare-release** is complete. Both stages are **operational** -- they push, queue pipelines, publish, and promote rather than producing ordinary commits -- and every push, publish, and promotion is **irreversible**. Run each stage only on explicit user instruction, and pause for confirmation before every irreversible action.

## Stage 1 - Land

Land the prepared `stage-release-<major>.<minor>` branch on `internal/release/<major>.<minor>`, gated on a green official build, then run the official build on the release branch that produces the `PackageArtifacts`. Never auto-complete a pull request that needs elevation.

Read and follow [references/stage-1-land.md](references/stage-1-land.md).

## Stage 2 - Publish and Promote

Publish the packages to nuget.org (the user runs `dotnet nuget push`; the agent never handles API keys), then promote the shipping build to the public `.NET <major>` channel. The internal build is auto-assigned only to the `.NET <major> Internal` channel (whose symbols go to an internal isolated feed), so promoting it to the public `.NET <major>` channel is a **required manual step** -- without it the shipped packages have no public symbols. The agent runs the promotion only after the user confirms the BAR id and target channel (irreversible once packages flow).

Read and follow [references/stage-2-publish-and-promote.md](references/stage-2-publish-and-promote.md).

## Next

Once the packages are published and the build promoted, run **validate-release** to verify the symbols on msdl and reconcile the branches.
