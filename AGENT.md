# AGENT.md — deskaway-desktop

The Windows host. It is the only component that actually does anything to a
real machine: it runs commands, captures their output, and enforces the
scope and pause controls that keep a run inside its boundaries.

This is the highest-risk repo in the system. Everything it executes came
from a model via the network, so the guard rails in `Core/Scope`,
`Core/Pause` and the approval flow are the product, not overhead.

## Folder structure

```
.github/workflows/
  ci.yml  release.yml  security.yml
DeskAway.sln
build/
  Directory.Build.props   # settings shared by every project
  version.props           # single source of the version number
  .editorconfig           # formatting and analyzer rules
src/
  DeskAway.Desktop.App/           # UI shell
    Startup/  Views/  ViewModels/
  DeskAway.Desktop.Core/          # run orchestration, no I/O
    AgentLoop/                    # step -> approve -> execute -> report
    Checklist/                    # plan state as steps complete
    Scope/                        # what this run may touch
    Pause/                        # stop and resume mid-run
    Journal/                      # local record of what happened
  DeskAway.Desktop.Execution/     # the dangerous part
    PowerShellRunner.cs           # command invocation
    ProcessTree/                  # child processes, kill whole trees
    OutputCapture/                # stdout/stderr streaming
    Timeouts/                     # nothing runs forever
    WorkingDirectory/             # resolve and constrain cwd
  DeskAway.Desktop.Transport/     # relay link
    RelayConnection.cs
    Handlers/  Heartbeat/  Reconnect/
  DeskAway.Desktop.Pairing/       # phone pairing
  DeskAway.Desktop.Storage/       # local state, credentials
  DeskAway.Desktop.Telemetry/
  DeskAway.Desktop.Updater/
tests/
  DeskAway.Desktop.Core.Tests/
  DeskAway.Desktop.Execution.Tests/
  DeskAway.Desktop.Transport.Tests/
  DeskAway.Desktop.Contract.Tests/   # conformance to deskaway-protocol
installer/
  msix/  signing/  uninstall/
docs/
  architecture.md  adr/
```

Directories holding only a `.gitkeep` are agreed structure with no project
file yet — several of those are whole projects not yet added to the solution.

## Conventions

- `Core/` is pure orchestration: no process launching, no sockets, no disk.
  It is the layer that must be testable without a machine to break.
- Every execution path goes through `Execution/`, and everything in there
  respects `Timeouts/` and `ProcessTree/`. A command that can outlive its run
  or orphan a child process is a bug, not an edge case.
- A step that `Core/Scope` cannot place inside the run's scope does not run.
  Fail closed, always.
- `Pause` must take effect between steps without losing checklist state.
- Wire shapes come from `deskaway-protocol`; `Contract.Tests` exists to catch
  drift. Never hand-roll a message type here.
- Version numbers change in `build/version.props` only.
- Never commit signing material. `.gitignore` blocks `*.pfx` and `*.snk`;
  keep it that way.

## Rule: keep README.md current

The README is the one file a newcomer is guaranteed to read. Revisit it
whenever this repo's answer to any of the four questions below changes — not
on a schedule.

Every DeskAway README answers four things, in this order:

1. **What this one repo is**, in two lines, and where it sits in the whole
   system.
2. **Its current status**, stated honestly. Right now that is *early
   development, nothing works yet.*
3. **How to run it locally**, aiming for under ten minutes.
4. **A link back** to the org or to `deskaway-docs`, so someone landing here
   can find the rest.

How to apply it:

- Keep those four as the first four sections, in that order. Anything else
  goes after them.
- Status rots fastest. The moment the first thing in this repo actually
  runs, that line changes in the same PR. "Nothing works yet" is honest
  only until it isn't.
- If a setup step breaks, or creeps past ten minutes, fix the README in the
  PR that caused it. A stale run section is worse than no run section.
- Never write intent as if it were fact. Anything not yet true is either
  labelled as planned or left out entirely.
- Two lines means two lines. If section 1 needs a third paragraph, that
  content belongs in `deskaway-docs`.

## Rule: keep CHANGELOG.md current

Add a line the moment you do something notable — not at release time. The
changelog is cheap to maintain one entry at a time and miserable to
reconstruct from five months of git log.

- Format is [Keep a Changelog](https://keepachangelog.com/en/1.1.0/);
  versioning is [SemVer](https://semver.org/spec/v2.0.0.html).
- Everything lands under `## [Unreleased]`, grouped by `### Added`,
  `Changed`, `Deprecated`, `Removed`, `Fixed`, or `Security`. Create a group
  when you first need it.
- "Notable" means a reader of this repo would want to know: a new capability,
  a behaviour change, a dependency that changes how you run it, a security
  fix. Not: formatting, a typo, an internal rename nobody outside the file
  can see.
- Write for someone who has not read the diff. "Added pairing code
  expiry" beats "updated code-generator.ts".
- On a release, rename `[Unreleased]` to the version with the date, and open
  a fresh empty `[Unreleased]` above it. Never delete history.
- Entries are past tense and one line. If yours needs a paragraph, it is
  probably two entries.

## Rule: keep the pull request template useful

`.github/pull_request_template.md` pre-fills every PR description. While this
project is one person reviewing their own work, it is the self-check that
catches what you were about to skip — so fill it in honestly rather than
deleting the prompts.

- Answer all four. "N/A" is a fine answer; a blank section is not.
- **Which unit of the plan this belongs to** is the one that pays off later.
  In five months this is how you find which PR did what, so name the unit,
  not the file you touched.
- **Anything deliberately left incomplete** is not an admission. An
  acknowledged gap is a decision; an unmentioned one is a bug you will
  rediscover.
- Change the template when a prompt stops earning its place, and keep it at
  four or five. A template long enough to skim past is worse than none.

## Rule: keep CONTRIBUTING.md short

It is currently five lines because there are no outside contributors. Resist
growing it for people who do not exist yet.

- Update it when the real answer changes: the formatter command, the branch
  rule, or the day the project starts accepting outside contributions.
- Anything longer than a few lines is either repo guidance — which belongs in
  this file — or cross-repo process, which belongs in `deskaway-docs`.

## Rule: keep SECURITY.md honest

One line in it will become false, and it is the important one.

- The supported-versions table says *nothing is supported, do not run this*.
  The day a version is tagged, that table changes in the same PR — an
  unsupported-looking project that is actually shipping teaches people to
  ignore the file.
- The reporting route assumes GitHub private vulnerability reporting is
  enabled on the repo. If that is ever turned off, this file needs a real
  contact route the same day, or reports arrive as public issues.
- Do not soften the warning about executing model-authored shell commands
  while the scope, approval and timeout controls are still unwritten. It is
  the most accurate sentence in the repo.
