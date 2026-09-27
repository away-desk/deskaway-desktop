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
