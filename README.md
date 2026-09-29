# deskaway-desktop

The Windows host that executes a run: it invokes commands, captures output,
and enforces the scope, timeout and pause controls around them.

It connects out to `deskaway-relay` and is driven from a phone. This is the
only component that touches a real machine, so its guard rails matter more
than its features.

## Status

**Early development. Nothing executes yet.**

The one thing that builds is the wire contract: `DeskAway.Protocol`, with
hand-written types for every protocol message and a C# port of the message
checker, and `Contract.Tests`, which holds them to `deskaway-protocol` on every
build. Every other project directory is still empty. `PowerShellRunner.cs` and
`RelayConnection.cs` are empty files. Nothing executes, and nothing is signed or
packaged.

## Running locally

You need the .NET 10 SDK on Windows (pinned in `global.json`). What runs
today is the build and the contract tests; the first build downloads the
protocol contract from GitHub. The intended path, once the app exists:

```sh
git clone https://github.com/away-desk/deskaway-desktop.git
cd deskaway-desktop

dotnet restore
dotnet build

dotnet test                                        # no machine side effects
dotnet run --project src/DeskAway.Desktop.App      # UI shell
```

Target is under ten minutes on a cold clone, restore included. A local
`deskaway-relay` is needed to pair and run anything end to end, but building
and testing must never require one — that is what `Core/` being I/O-free
buys. `docs/local-setup.md` has the detail, including how the protocol
contract is pinned.

**A warning worth keeping here:** this component runs commands on your own
machine. Once it works, do not point a development build at a real desktop
you care about without reading the scope controls first.

## The rest of DeskAway

Cross-repo docs and architecture decisions live in
**[deskaway-docs](https://github.com/away-desk/deskaway-docs)**. All
components are under the **[away-desk](https://github.com/away-desk)** org.

The wire protocol this client speaks is defined in
[deskaway-protocol](https://github.com/away-desk/deskaway-protocol).
Contributor guidance, including the rule for maintaining this README, is in
[AGENT.md](./AGENT.md).
