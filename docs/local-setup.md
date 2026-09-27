# Local setup — deskaway-desktop

**Nothing builds yet.** `DeskAway.sln` references no projects, because no
`.csproj` files exist. This page describes the setup as it is intended to work.
Correct it in the same pull request that makes it true.

> **Before you run this component, read the warning at the bottom of this page.**
> This is the part of DeskAway that executes commands on a real machine.

## What you need

- Windows.
- A recent .NET SDK (the `ci.yml` workflow currently targets 8.0.x — keep the
  two in step).
- Visual Studio or Rider if you want a designer and debugger; the command line is
  enough otherwise.

## Steps

```sh
git clone https://github.com/away-desk/deskaway-desktop.git
cd deskaway-desktop

dotnet restore
dotnet build
```

Run the tests before running the app — they are the part that cannot damage
anything:

```sh
dotnet test
```

Then the app itself:

```sh
dotnet run --project src/DeskAway.Desktop.App
```

Target for the whole sequence on a cold clone is under ten minutes, most of it
`restore`.

## Formatting and analyzers

```sh
dotnet format                      # apply
dotnet format --verify-no-changes  # what CI checks
```

Rules come from `build/.editorconfig` and apply to every project through
`build/Directory.Build.props`. Version numbers live only in
`build/version.props`.

## Running against a relay

Pairing and running a task end to end needs a reachable `deskaway-relay`. Point
this client at a local one rather than the deployed environment while developing.

**Building and testing must never require a relay.** `Core/` performs no I/O
precisely so that the orchestration logic — the agent loop, the checklist, scope
and pause — can be tested with no network and no machine to break. If a test in
`Core.Tests` starts needing a socket, the dependency has leaked the wrong way.

`Contract.Tests` checks conformance against `deskaway-protocol`. When it fails
after a protocol change, the fix usually belongs there rather than here.

## Warning: this component runs commands on your machine

Once `Execution/` is implemented, this application invokes shell commands that
originated from a model, reached over a network. The controls that make that
acceptable — `Core/Scope`, the approval gate, `Execution/Timeouts`,
`Execution/ProcessTree` — are the parts still unwritten.

While that is true:

- Run it on a throwaway virtual machine, not on your own desktop.
- Do not point a development build at a real user's machine.
- Assume a bug in `Scope` means a command runs somewhere you did not intend.

An unfinished DeskAway is not a limited DeskAway; it is remote command execution
with the safety features missing. See [SECURITY.md](../SECURITY.md).

## When it will not build

- **`dotnet build` succeeds but the solution is empty** — expected today; no
  projects exist yet.
- **Analyzer errors after pulling** — `build/.editorconfig` changed. Run
  `dotnet format` rather than suppressing them one by one.
- **A test hangs** — suspect `Execution/Timeouts` or an orphaned process from an
  earlier run; check for stray child processes before re-running.
