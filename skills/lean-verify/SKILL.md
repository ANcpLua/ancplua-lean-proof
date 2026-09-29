---
name: lean-verify
description: Prove C#/.NET state machines correct with Lean 4 and turn every counterexample into a reproduced, fixed bug. Use when asked to model or prove code in Lean, to hunt race conditions or state bugs with proofs, or to compare an existing Lean model with the code.
argument-hint: "<state machines, types or files>"
---

# Lean proof campaign

Targets: $ARGUMENTS
None listed: take those named in the request. None named: propose candidates (state written from several methods, retry and backoff loops, shutdown, drain and flush paths, cancellation and approval flows) and start with the riskiest.

The campaign is a pipeline. Each step has one input and one output, shown in its heading. Lean proves the model; the anchor, the replayed traces and the tests tie the model to the code. The campaign ends when every target has an anchored model, proofs that pass the audit, every counterexample reproduced and fixed in a draft PR or reported spurious, and a report a skeptic can rerun.

Tested on .NET SDK 10.0.401 (runtime 10.0.12), TFM `net10.0`, MSTest.Sdk 4.4.1 on Microsoft.Testing.Platform 2.4.1, and Lean 4.34.1.

## 1. Anchor: code → write sites
`dotnet run ${CLAUDE_SKILL_DIR}/scripts/state-writes.cs -- <project|sln|slnx> <Namespace.Type.member>... [--expect cited.json]`, from the repo root so its global.json picks the SDK.
- Pick the fields and properties each machine owns; the script lists every write site as JSON with stable ids.
- A citations file per machine maps each site id to a model transition or a declared abstraction. `--expect` fails on uncited sites, stale ids and drifted members; rerun it after every code change.
- Exit 0 is evidence. 3: the code didn't compile. 4: symbol not found. 5: zero sites, which means a wrong symbol. 6: citations out of date.

## 2. Model: write sites → Lean model
- One model per machine. Theorems and trace replay share one `step` function.
- Write down the model's assumptions, its abstractions and what it doesn't prove.

## 3. Specify: contract → claims
- Claims are closed `Prop` definitions in a `Spec` namespace, written from the contract (docs, protocol, issues), not from the implementation. Proofs are `theorem … : Spec.X`, so no claim weakens while it is proved. Editing a Spec file reopens review.
- Per claim: a witness that its hypotheses hold, and a deliberately broken model that breaks it.

## 4. Replay: real runs → accepted or rejected traces
- Replay real runs (test runs, local runs, telemetry; no test-only hooks in production) through the same `step`.
- A rejected trace means the model or the code is wrong, and that model's proofs wait until it's resolved.
- Report traces accepted per model and how much code the runs covered.

## 5. Review: model → skeptic's report
Hand each model to the skeptic agent with the claims, artifact paths and rerun commands, not your reasoning. Fixes get the same review. Resolve every BROKEN before reporting.

## 6. Reproduce: counterexample → failing test
- A counterexample is a finding once it reproduces as a failing MSTest test at the owner boundary on the unfixed code, failing for the stated reason. Otherwise it's spurious: the model over-approximates there.
- For each finding: who can hit it today, and through which path.

## 7. Fix: finding → draft PR
- One draft PR per fix. The fixed model proves the claim and still matches the fixed code, by anchor and traces.
- A fix that covers only part of the proved variant is incomplete: say so and open the follow-up.
- New tests fail on the pre-fix code, or carry their own justification.

## 8. Detect: proved claim → killed mutants
Coverage shows that code ran, not that a test would notice it changing.
- Mutate each claim's write sites and guards with Roslyn, one mutant at a time, in a worktree of the committed baseline. Predict the failing test first.
- A run counts when the mutant's assembly hash differs from the baseline built in the same worktree and the expected tests ran (`--minimum-expected-tests`). Outcomes: killed, survived, no run (doesn't compile, unreached, infrastructure).
- A survivor gets a test or a written equivalence argument. After restoring, the rebuilt assembly hash equals the baseline again.

## 9. Simplify: proofs → merged paths
Proofs license merging duplicated logic into one path; reachability shows what can't be deleted. Simplification PRs leave tests unchanged. Rerun the anchor, the audit, the traces and the tests.

## 10. Audit: Lake project → pass or fail
`${CLAUDE_SKILL_DIR}/scripts/lean_audit.sh <RootModule> [--spec <Namespace>]` from the Lake project root: build, axiom and spec audit, then a leanchecker kernel replay of every module.
A green `lake build` alone proves little: `sorry` only warns, `native_decide` and project axioms are silent, and a kernel bypass through `debug.skipKernelTC` passes both the build and the axiom check. Only the kernel replay catches it. Run leanchecker only with a module argument; without one it replays the entire search path.

## Report
Claims that held, findings with PRs, spurious counterexamples, provisional models, non-goals. Counts in defined units: PRs by type, bugs by source (proofs, review, gaps in your own fixes), non-test and test lines ±, test cases ±, theorems, traces accepted, mutants killed and survived. Include the rerun commands. Publish or merge only when the owner says so.
