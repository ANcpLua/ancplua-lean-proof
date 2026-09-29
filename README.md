# ANcpLua Lean Verify

Prove the state machines in a C#/.NET codebase correct with Lean 4, and turn every counterexample into a bug that a failing MSTest test reproduces and a draft pull request fixes.

Lean proves the model. The plugin ties the model to the code: Roslyn lists every write site of the modelled state and cites it to a model transition, real runs replay through the model, an independent skeptic agent reviews each model, and a final audit rebuilds the proofs, checks their axioms and replays every module through the Lean kernel.

## The pipeline

Each step has one input and one output.

| Step | Input → output |
| --- | --- |
| Anchor | code → write sites, each cited to a model transition |
| Model | write sites → Lean model |
| Specify | contract → claims |
| Replay | real runs → accepted or rejected traces |
| Review | model → skeptic's report |
| Reproduce | counterexample → failing test |
| Fix | finding → draft pull request |
| Detect | proved claim → killed mutants |
| Simplify | proofs → merged code paths |
| Audit | Lean project → pass or fail |

## Use

Ask Claude to prove a state machine, or name the types or files:

```
/ancplua-lean-verify:lean-verify MyApp.Orders.OrderSaga
```

Without targets, the skill proposes candidates and starts with the riskiest: state written from several methods, retry and backoff loops, shutdown and drain paths, cancellation and approval flows.

## Inside

- **Skill `lean-verify`**: the pipeline above.
- **Agent `skeptic`**: tries to break a model, proof, fix or test, and reports only what it can reproduce.
- **`scripts/state-writes.cs`**: the Roslyn inventory of write sites, as JSON with stable ids.
- **`scripts/lean_audit.sh`** and **`scripts/axiom_audit.lean`**: build, axiom and spec audit, and the kernel replay.

## Requirements

- Claude Code: the skill runs .NET and Lean on your machine, and the skeptic agent is a Claude Code component
- .NET SDK 10.0.1xx or later (the inventory is a file-based app run with `dotnet run`)
- Lean 4 with `elan` and Lake
- git; a Git remote and the GitHub CLI for the draft pull requests

Tested on .NET SDK 10.0.401 (runtime 10.0.12), MSTest.Sdk 4.4.1 on Microsoft.Testing.Platform 2.4.1, and Lean 4.34.1.

## What it runs, fetches and sends

- `dotnet run state-writes.cs` restores Microsoft.CodeAnalysis.Workspaces.MSBuild 5.9.0 and Microsoft.CodeAnalysis.CSharp.Workspaces 5.9.0 from nuget.org, loads your project or solution with MSBuild (which evaluates your build files) and prints its inventory to standard output.
- `lean_audit.sh` runs `lake build`, `lake env lean --run axiom_audit.lean` and `lake env leanchecker` in your Lake project. `lake build` fetches the dependencies your lakefile declares.
- The skill has Claude build and test your code, create git worktrees for mutants and pre-fix runs, and open draft pull requests on your repository's remote. It publishes or merges a pull request only when you say so.
- The plugin has no hooks, no MCP servers and no telemetry, and sends no data anywhere on its own.

## License

MIT, see [LICENSE](LICENSE).
