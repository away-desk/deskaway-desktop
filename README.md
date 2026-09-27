# deskaway-desktop

The Windows host that executes a run: it invokes commands, captures output,
and enforces the scope, timeout and pause controls around them.

It connects out to `deskaway-relay` and is driven from a phone. This is the
only component that touches a real machine, so its guard rails matter more
than its features.

## Status

**Early development, nothing works yet.**

The solution file and every project directory are in place but empty — no
`.csproj` exists yet, so the solution references nothing and does not build.
`PowerShellRunner.cs` and `RelayConnection.cs` are empty files. Nothing
executes, and nothing is signed or packaged.

## Running locally

Not yet possible: there are no project files, so `dotnet build` has nothing
to compile.

You will need a recent .NET SDK on Windows. The intended path, once the
projects exist:

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
buys. `docs/architecture.md` will carry the detail; it is currently empty.

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
