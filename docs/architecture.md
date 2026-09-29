# Architecture — deskaway-desktop

## Responsibility

Runs the plan on a real Windows machine. It holds an outbound connection to
`deskaway-relay`, receives steps, gates the ones needing approval, executes what is
approved, streams output back, and stops when told to stop.

It is the only component that touches a real machine, which makes its restraints
more important than its capabilities.

## What it deliberately does not do

- **It does not plan, and it never talks to a model.** It receives steps it did not
  write and holds no provider key. If it could generate its own next step, every
  control below would be advisory.
- **It never accepts a provider credential from the relay.** Not for a retry, not
  for a "local fallback", not to avoid a round trip. A desktop that can call a
  model is a desktop that can plan its own actions.
- **It does not trust the plan.** A step arriving over the network is a request, not
  an instruction. Scope is checked locally, and a step that cannot be placed inside
  the run's scope does not run. Fail closed, always.
- **It accepts no inbound connections.** The socket to the relay is outbound only.
  There is no port to reach on a user's machine, and the phone never connects
  directly.
- **It does not run anything unapproved when the autonomy level requires
  approval.** The gate is a block, not a notification.
- **It does not classify reversibility.** Tiers arrive with the plan. The desktop
  enforces; it does not judge.
- **It is not the system of record.** `Core/Journal` is a local record for the
  user's benefit; the relay holds the authoritative transcript.

## Internal pieces, and how a message flows

**`Core/`** — orchestration with no I/O. `AgentLoop` drives take-a-step, gate it,
execute it, report. `Checklist` tracks plan state. `Scope` decides what this run may
touch. `Pause` stops between steps without losing state. `Journal` records locally.

**`Execution/`** — the part that can break a machine, kept small.
`PowerShellRunner` invokes; `ProcessTree` tracks children so a kill takes the whole
tree; `OutputCapture` streams stdout and stderr as they happen; `Timeouts`
guarantees nothing runs forever; `WorkingDirectory` resolves and constrains where a
command runs.

**`Transport/`** — `RelayConnection` plus `Handlers`, `Heartbeat`, and `Reconnect`
for the laptop that closed its lid.

**`DeskAway.Protocol`** — the wire contract in C#: hand-written records for every
envelope and payload, and `MessageChecker`, a port of the protocol's check order
(size, parse, version, relay block, type, envelope, payload) validating against
schemas embedded at build time. The schemas and examples come from the
`deskaway-protocol` commit pinned in `build/protocol.props`; the build downloads
them into `.protocol/<commit>/`. It is a shared contract rather than a desktop
layer, hence the name, which is also the NuGet package it becomes on Day 20.

**`Pairing/`**, **`Storage/`** (local state and credentials), **`Telemetry/`**,
**`Updater/`**, and **`App/`** (the UI shell: `Views`, `ViewModels`, `Startup`).

### Flow of one command step

1. `Transport/RelayConnection` receives a `command-request` frame and a handler in
   `Transport/Handlers` passes it through `DeskAway.Protocol`'s `MessageChecker`,
   which validates it against the protocol schema and returns a typed record.
2. The handler hands a plain domain object to `Core` — **not** the frame. Transport
   types stop at this boundary.
3. `Core/AgentLoop` takes the step and asks `Core/Scope` whether it is inside this
   run's boundaries. If not, the step is refused locally and reported as refused;
   nothing is executed.
4. If the step's tier requires approval, `AgentLoop` emits an approval request back
   through `Transport` and **waits**. `Core/Pause` can interrupt here.
5. On approval, `Core` calls `Execution/` with a command, a working directory, and
   a timeout. `Core` does not know it is PowerShell.
6. `Execution/PowerShellRunner` launches under `ProcessTree` supervision.
   `OutputCapture` streams output back as it arrives; `Timeouts` kills the tree if
   it overruns.
7. `Core/Checklist` records the outcome, `Core/Journal` appends it locally, and the
   result goes out through `Transport` to the relay.

The shape is **Transport → Core → Execution**, with results flowing back the same
way. Nothing skips `Core`.

## Layering rules

**`Core` compiles with no reference to `Transport` or `Execution`.** This is the
hard rule of this repo, and it is enforced at the project-reference level so that
breaking it fails the build rather than a review.

The reason: `Core` holds the logic that decides whether something dangerous
happens — scope checks, the approval gate, pause and resume, checklist state. That
logic must be testable exhaustively, and it can only be tested exhaustively if
running the tests requires no socket and no machine to damage.
`Core.Tests` should be able to run a hundred adversarial plans through `AgentLoop`
in a second, on any machine, with no risk. The moment `Core` references
`Execution`, testing a scope refusal means being one bug away from actually
executing the command you were trying to prove would be refused.

Dependencies point one way: `App` → `Core`; `Transport` → `Core`; `Execution` →
`Core`. Both outer layers depend on `Core`, and `Core` depends on neither.
`Transport` and `Execution` must not reference each other either — a frame must
never reach a process launcher without passing through the logic in between.

Two supporting rules:

- **Only `Transport` references `DeskAway.Protocol`, and `DeskAway.Protocol`
  references nothing in this solution.** Wire records are frames; a frame is
  translated to a domain object at the `Transport` edge, so `Core` and
  `Execution` never see one. `Contract.Tests` fails if any other project
  references it.
- **Handlers translate, they do not decide.** A `Transport` handler converts a
  frame to a domain object and hands it on. Any branch on whether to run something
  belongs in `Core`.
- **Version numbers change only in `build/version.props`**, which every project
  imports through `build/Directory.Build.props`.

## What it talks to, and in which direction

| Direction | Peer | Over |
| --- | --- | --- |
| **outbound** | `deskaway-relay` | WebSocket, dialled out and held open |
| **outbound** | update service | HTTPS, via `Updater/` |
| **local** | the machine itself | processes, filesystem, via `Execution/` |
| — | `deskaway-protocol` | build-time only: schemas and examples downloaded at the commit pinned in `build/protocol.props`; the hand-written records are held to them by `Contract.Tests` on every build |

There is no inbound direction at all. It never talks to the phone, never to
`deskaway-agent`, and never to a model provider — the relay is its only peer.
