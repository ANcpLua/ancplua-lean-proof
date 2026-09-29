---
name: lean-verify
description: Formally verify C#/.NET state machines with Lean 4 and turn proof counterexamples into reproduced, fixed bugs. Use when asked to formally verify, prove, or model code in Lean, to hunt race conditions or state bugs with proofs, or to check an existing Lean model against the code.
argument-hint: "<state machines, types or files to verify>"
---

# Lean verification campaign

Targets: $ARGUMENTS
If none are listed, use the ones named in the request. If none are named, propose candidates (state fields written from several methods, retry and backoff loops, shutdown, drain and flush paths, cancellation and approval flows) and start with the riskiest.

Done when every target has a Lean model anchored to the code, proofs that pass the final audit, every counterexample either reproduced as a failing MSTest test and fixed in a draft PR or reported as spurious, and a report a skeptic can rerun. Lean proves the model; the anchor, the trace replay and the tests are what connect the model to the code, so none of them is optional.

## Tools in this skill
Tested on .NET SDK 10.0.401 (runtime 10.0.12), TFM `net10.0`, with MSTest.Sdk 4.4.1 on Microsoft.Testing.Platform 2.4.1 (API docs: `view=mstest-net-4.4`), and Lean 4.34.1.
- `dotnet run ${CLAUDE_SKILL_DIR}/scripts/state-writes.cs -- <project|sln|slnx> <Namespace.Type.member>... [--expect cited.json]`
  Roslyn inventory of every write site of the given fields or properties, as JSON with stable ids. Exit 0 ok, 3 load or compile errors, 4 symbol not found, 5 zero sites, 6 citations uncited, stale or drifted. Run it from the repo root so the repo's global.json picks the SDK.
- `${CLAUDE_SKILL_DIR}/scripts/lean_audit.sh <RootModule> [--spec <Namespace>]`
  From the Lake project root: build, axiom and spec audit, leanchecker replay of every module.

## 1. Anchor
- Pick the state (fields, properties) each machine owns and inventory its write sites.
- Keep a citations file per machine: every site id maps to a model transition or a declared abstraction. `--expect` fails on uncited sites (the model is missing behavior), stale ids, and drifted members (the code changed under a citation). Rerun it after every code change; a failure reopens the model.
- The inventory is evidence only when it exits 0. Exit 3 means the semantic model saw errors; zero sites means a wrong symbol, not immutable state.
- Navigate with the C# LSP, but cite the inventory: it is compile-checked and rerunnable.

## 2. Model and specify
- One model per machine. Theorems and trace replay use the same `step` definitions; otherwise a replay says nothing about what was proved.
- Claims are closed `Prop` definitions in a `Spec` namespace, written from the contract (docs, protocol, issues), not from the implementation. Proofs are `theorem … : Spec.X`, so a claim can't quietly weaken while it is being proved. Editing a Spec file reopens review.
- Per claim: a non-vacuity witness (the hypotheses are satisfiable) and a sensitivity check (a deliberately broken model violates the claim).
- Per model, write down its assumptions, its abstractions, and what it does not prove.

## 3. Conform
- Replay real traces (test runs, local runs, existing telemetry; no test-only production hooks) through the model. A rejected trace means the model or the code is wrong, and that model's proofs are provisional until it is resolved.
- Proofs transfer to the code only if the model admits every real behavior: citations cover that statically, traces dynamically. Report how many traces each model accepted and how much of the code the trace runs covered; two accepted traces is thin evidence.

## 4. Review
Hand each model to the skeptic subagent with the claims, artifact paths and rerun commands, and without your reasoning. Fixes get the same review. Resolve every BROKEN before reporting.

## 5. Findings and fixes
- A counterexample becomes a finding once it reproduces as a failing MSTest test at the owner boundary on the unfixed code, failing for the stated reason. Otherwise report it as spurious: the model over-approximates there.
- For each finding, say who can hit it today and through which path.
- One draft PR per fix. Prove the fix: the fixed model satisfies the claim and still matches the fixed code (inventory and traces). A fix that covers only part of the proved variant is incomplete; say so and open the follow-up.
- New tests in a fix PR either fail on the pre-fix code or carry their own justification.

## 6. Detection
Coverage shows that code ran, not that a test would notice it changing: in a fixture with 100% line and branch coverage, a mutant on a covered line survived every test.
- Per proved claim, mutate its write sites and guards with Roslyn, one mutant at a time, in a worktree of the committed baseline. Write down the predicted failing test before each run.
- A run counts when the mutant's assembly hash differs from the baseline built in the same worktree (deterministic builds make this exact) and the expected tests executed (`--minimum-expected-tests`). Verdicts: killed, survived, invalid (doesn't compile, unreached, infrastructure).
- A survivor needs a test or a written equivalence argument.
- After restoring, rebuild: the assembly hash must equal the baseline again.

## 7. Simplify
Proofs license merging duplicated logic into one path; reachability shows what can't be deleted. Simplification PRs leave tests unchanged. Rerun the anchor check, the audit, the traces and the tests.

## 8. Final audit
Run `lean_audit.sh`. A passing `lake build` proves little on its own: sorry only warns, native_decide and project axioms are silent, and a kernel bypass through `debug.skipKernelTC` passes both the build and the axiom check; only the leanchecker replay catches it. Never run leanchecker without a module argument, since it then replays the entire search path.

## Report
Held-up claims, findings with PRs, spurious counterexamples, provisional models, non-goals. Counts in defined units: PRs by type, bugs by source (proofs, review, gaps in your own fixes), non-test and test LOC ±, test cases ±, theorems, traces accepted, mutants killed and survived. Include the rerun commands. Publish or merge only when the owner says so.
