# Local setup — deskaway-desktop

**What builds today** is `DeskAway.Protocol` (the wire types and message
checker) and `DeskAway.Desktop.Contract.Tests`. The app, transport and execution
projects do not exist yet; the steps that run the app describe the setup as it
is intended to work. Correct them in the pull request that makes them true.

> **Before you run this component, read the warning at the bottom of this page.**
> This is the part of DeskAway that executes commands on a real machine.

## What you need

- Windows.
- The .NET 10 SDK. `global.json` pins it, and CI installs the same version from
  that file: `winget install Microsoft.DotNet.SDK.10`.
- Network access to github.com on the first build: it downloads the protocol
  contract (below).
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

How to tell it is working: the first build prints `Downloading the protocol
contract at <commit>`, and `dotnet test` ends with `Test run summary: Passed!`
and `failed: 0`. Today that is 43 tests, including one per protocol example
(9 valid, 17 invalid).

`dotnet test` uses the Microsoft Testing Platform, which `global.json` opts
into; the .NET 10 SDK no longer runs xUnit v3 through VSTest.

## The protocol contract

The C# wire types are hand-written, so they are held to `deskaway-protocol` by
tests rather than by a generator. Those tests need the protocol's schemas and
examples, which the build fetches:

- `build/protocol.props` pins one commit (`DeskAwayProtocolCommit`).
- The first build downloads that commit from GitHub into `.protocol/<commit>/`
  (gitignored) and embeds the schemas into `DeskAway.Protocol`. The folder is
  named after the commit, so a stale download is never reused.
- A missing download, a wrong commit, or an empty examples folder fails the
  build with a message saying which.

**Moving to a newer protocol** is a one-line change to `build/protocol.props`,
in a pull request of its own. If `Contract.Tests` then fails, the protocol
changed something the C# has not caught up with — which is the point: update the
records in `DeskAway.Protocol` from the schema, field by field.

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

- **`Failed to download file ... 404`** — `DeskAwayProtocolCommit` names a
  commit that does not exist in `deskaway-protocol`. Check the SHA.
- **`must be a full 40-character commit SHA`** — the pin was shortened. Use
  the full SHA.
- **`Testing with VSTest target is no longer supported`** — you are running
  `dotnet test` without `global.json`'s test-runner setting, or on an SDK older
  than 10.
- **Analyzer errors after pulling** — `build/.editorconfig` changed. Run
  `dotnet format` rather than suppressing them one by one.
- **A test hangs** — suspect `Execution/Timeouts` or an orphaned process from an
  earlier run; check for stray child processes before re-running.
