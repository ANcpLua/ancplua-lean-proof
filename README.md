# ANcpLua Lean Verify

Formally verify the state machines in a C#/.NET codebase with Lean 4, and turn every counterexample the proofs produce into a bug that is reproduced by a failing MSTest test and fixed in a draft pull request.

Lean proves the model; the plugin's job is to keep the model tied to the code. Every write site of the modelled state is inventoried with Roslyn and cited to a model transition, real runs are replayed through the model, each model is reviewed by an independent skeptic agent, and a final audit rebuilds the proofs, checks their axioms and replays every module through the Lean kernel.

## What's inside

- **Skill `lean-verify`**: the campaign. Anchor the model to the code, write claims from the contract, replay real traces, review, reproduce and fix findings, measure detection with mutants, simplify, audit, report.
- **Agent `skeptic`**: an adversarial reviewer that tries to break a model, proof, fix or test and reports only what it can reproduce.
- **`scripts/state-writes.cs`**: Roslyn inventory of every write site of the given fields or properties, as JSON with stable ids; with `--expect`, fails on uncited, stale or drifted sites.
- **`scripts/lean_audit.sh`** and **`scripts/axiom_audit.lean`**: `lake build`, an axiom and spec audit, and a `leanchecker` kernel replay of every module.

## Use

Ask Claude to formally verify a state machine, or run the skill with the types or files to verify:

```
/ancplua-lean-verify:lean-verify MyApp.Orders.OrderSaga
```

Without targets, the skill proposes candidates: state written from several methods, retry and backoff loops, shutdown and drain paths, cancellation and approval flows.

## Requirements

- .NET SDK 10.0.1xx or later (the inventory is a file-based app run with `dotnet run`)
- Lean 4 with `elan` and Lake
- git; a Git remote and the GitHub CLI if you want the draft pull requests

Tested on .NET SDK 10.0.401 (runtime 10.0.12), MSTest.Sdk 4.4.1 on Microsoft.Testing.Platform 2.4.1, and Lean 4.34.1.

## What it runs, fetches and sends

- `dotnet run state-writes.cs` restores Microsoft.CodeAnalysis.Workspaces.MSBuild 5.9.0 and Microsoft.CodeAnalysis.CSharp.Workspaces 5.9.0 from nuget.org, loads your project or solution with MSBuild (which evaluates your build files) and prints its inventory to standard output.
- `lean_audit.sh` runs `lake build`, `lake env lean --run axiom_audit.lean` and `lake env leanchecker` in your Lake project. `lake build` fetches the dependencies your lakefile declares.
- The skill has Claude build and test your code, create git worktrees for mutants and pre-fix checks, and open draft pull requests on your repository's remote. It publishes or merges a pull request only when you say so.
- The plugin has no hooks, no MCP servers and no telemetry, and sends no data anywhere on its own.

## License

MIT, see [LICENSE](LICENSE).
