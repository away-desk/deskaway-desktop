# 0002. Hand-written wire types in DeskAway.Protocol, held to a contract downloaded at a pinned commit

- **Date:** 2026-09-29
- **Status:** Accepted
- **Supersedes:** none
- **Superseded by:** none

## Context

V1 generates types for TypeScript and Python only; the desktop's C# is written by
hand (`deskaway-protocol` ADR 0012). This is the component that runs commands on
a real machine, and hand-written types can drift from the schema where
generated ones cannot. The contract tests are the only thing holding that line,
and they need the protocol's schemas and examples — which live in another
repository, with no NuGet package until Day 20.

Three questions followed: where the types live, how the desktop gets the
protocol's files, and how strict the tests are.

## Decision

- **A project of its own, `DeskAway.Protocol`.** The wire records, a C# port of
  the protocol's `MessageChecker`, and the JSON options. It is named for the
  shared contract it is, not for the desktop layer it sits beside, and it is the
  NuGet package it becomes on Day 20. It references no other project here, and
  only `Transport` (and `Contract.Tests`) may reference it: a frame is
  translated at the `Transport` edge and never reaches `Core` or `Execution`.
  A test fails if that direction is broken.
- **The contract is downloaded at a pinned commit.** `build/protocol.props` holds
  one `deskaway-protocol` commit. The build downloads that commit's archive from
  GitHub into `.protocol/<commit>/`, embeds the schemas into `DeskAway.Protocol`,
  and fails if the download, the commit or the examples are missing. The folder
  is keyed by commit and marked complete only after a full download.
- **Every example, every build.** `Contract.Tests` runs each valid example
  (checker passes, typed records round-trip to identical JSON) and each invalid
  one (exact close reason) as its own test case; checks the C# enums against the
  protocol's enum files and each record's fields and required set against its
  schema; and fails if fewer cases ran than there are examples.

## Rejected options

- **A git submodule of `deskaway-protocol`.** A known source of pain: a submodule
  does not update when you pull, so tests quietly run against yesterday's
  fixtures; a clone without the extra flag leaves the folder silently empty, and
  CI is where that bites; and a branch switch can leave it pointing somewhere
  unexpected. That is a lot of machinery for a folder of test data. A download
  at a pinned commit gives the same guarantee with none of those edges.
- **CI checks out the protocol repo separately.** Same pinning, but local runs
  would need a manual clone kept in step by hand.
- **Copy the schemas and examples into this repo.** A third copy that drifts
  silently.
- **Put the types in `Transport/`.** They would then belong to a layer rather
  than to the contract, and a second consumer could not take them without taking
  `Transport`.
- **Generate the C#.** The safe option, deferred for time; see the protocol's
  ADR 0012 for when to revisit.

## Consequences

- The first build needs github.com. After that the download is cached per commit.
- Moving the protocol forward is a one-line pull request, checked by
  `Contract.Tests`; a new message type or field fails the build until the C# is
  written.
- The check order now exists in three languages. The shared examples keep them
  in agreement, and every break of the C# ordering tried so far was caught.
- Round-tripping exactly required two wire-faithful types: `UtcTimestamp` keeps
  timestamp text as sent, and `OptionalField<T>` keeps "absent" distinct from
  "null" (a heartbeat may send either).
