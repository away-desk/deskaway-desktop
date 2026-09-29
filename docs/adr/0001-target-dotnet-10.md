# 0001. Target .NET 10 LTS

- **Date:** 2026-09-29
- **Status:** Accepted
- **Supersedes:** none
- **Superseded by:** none

## Context

The first real projects in this repo are created in Unit 3. CI was pinned to
.NET 8, but .NET 8 leaves Microsoft support on 10 November 2026 — six weeks from
the day the first line of C# was written. .NET 10 is the current long-term
support release, supported until November 2028. There is no existing code to
migrate either way.

## Decision

Target `net10.0` everywhere, through `build/Directory.Build.props`. Pin the SDK
in `global.json` (`10.0.100`, rolling forward to the latest feature band), and
have CI install the SDK from that file, so local builds and CI cannot drift
apart.

`global.json` also opts `dotnet test` into the Microsoft Testing Platform,
which the .NET 10 SDK requires for xUnit v3.

## Rejected options

- **.NET 8.** Matches the old CI file, but would start a security-sensitive
  component on a runtime six weeks from its last security patch, and force a
  migration almost immediately.
- **.NET 9.** A standard-term release, out of support before .NET 8's
  replacement would even be needed.

## Consequences

- Supported until November 2028, well past V1.
- The .NET 10 SDK is required locally; older SDKs refuse the solution because
  of `global.json`.
- New .NET 10 serializer options are available and used: `AllowDuplicateProperties
  = false` and `RespectNullableAnnotations` make the wire types stricter.
- Revisit when .NET 12 LTS ships (November 2027).
