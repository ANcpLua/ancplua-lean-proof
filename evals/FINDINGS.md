# Why lean-proof loads thousands of times and runs four

Measured 2026-10-14 with Claude Code 2.1.299 and `claude-opus-5-5`. Dashboard figures, as given: by Oct 8, 416 installs (256 claude.ai chat, 98 Cowork, 62 Claude Code; 193 accounts' Claude Code got the chat install by sync), 13,085 loads, 0 load errors, 1 invocation; by Oct 10, 482 installs, 2 active accounts, 4 uses.

## Findings

1. **A load is a session, not a chance to use the skill.** Every session where a plugin is enabled carries its skill names and descriptions "whether or not the plugin gets used", and the `plugin_loaded` event fires "one per enabled plugin at session start" ([Measure plugin cost and usage](https://code.claude.com/docs/en/plugins/measure.md)). 13,085 loads over 416 installs in 8 days is about 31 per install, roughly 4 session starts a day. The portal's Usage tab doesn't define a load more precisely ([After publishing](https://claude.com/docs/connectors/building/after-publishing)), so treating its loads as session starts is an inference from the Claude Code telemetry definition. The load-to-use ratio is not a failure rate.

2. **Most installs are on a surface where the campaign can't run.** In chat, skills load but agents are ignored ([Platform support](https://claude.com/docs/plugins/platform-support.md)), and skills "run in Claude's code sandbox" ([Skills overview](https://claude.com/docs/skills/overview.md)): the user's repository and their .NET SDK, Lean toolchain and `gh` aren't there, and neither is the `skeptic` agent. That is 256 of the 416 installs (62%). Cowork loads agents and "works directly on your computer" ([Cowork overview](https://claude.com/docs/cowork/overview.md)), but no page fetched this session says whether a Cowork task can run `dotnet run` and `lake build`, so Cowork is unverified. Claude Code loads everything, and an account install also reaches Claude Code "as a synced plugin at the next session start" ([Platform support](https://claude.com/docs/plugins/platform-support.md)). Those synced sessions add loads without adding people who meant to run a Lean campaign there.

3. **How Claude picks a plugin skill.** Claude sees each skill's name and description; the body loads only on invocation. The description is "the only part Claude sees before deciding" ([Skills overview](https://claude.com/docs/skills/overview.md)). The listing has a character budget of 1% of the context window. When the listing overflows, Claude Code "drops descriptions starting with the skills you invoke least" and keeps only the name, and each entry is capped at 1,536 characters ([Skills: descriptions are cut short](https://code.claude.com/docs/en/skills.md)). A newly installed skill that nobody has invoked yet is first in line to lose its description.

4. **Checked on this machine.** `claude --plugin-dir . -p /context` reports a 1M context for Opus, Sonnet and Haiku 5.5. The listing has 30 skills at 3.8k tokens, and `ancplua-lean-proof:lean-proof` keeps its full description (~100 tokens). With `SLASH_COMMAND_TOOL_CHAR_BUDGET=1500` the listing drops to 2.1k tokens. `lean-proof` falls to name-only (< 20 tokens) along with every user and plugin skill, while the built-in skills keep their text. `claude plugin details` puts the plugin's always-on cost at ~214 tokens (110 skill, 110 agent).

5. **With its description visible, the skill fires when the task comes up.** The baseline below fired in 36 of 36 should-trigger runs, including 18 of 18 from the six prompts that never say Lean, proof or prove, and in 0 of 36 should-not-trigger runs. The current description is not the bottleneck in an uncluttered listing. Two limits apply. Eval runs load only the plugin under test, with "other installed plugins … and skills … absent" ([Plugin evals](https://code.claude.com/docs/en/plugin-evals.md)). And each prompt states its intent and carries its code.

6. **Pending: what a name-only listing does.** A scratch copy with `description: lean-proof` (the closest stand-in for a dropped description) is being run on the 12 should-trigger cases. Its first two cases, both naming Lean, fired 3/3. The six plain-language cases are the ones to read when it finishes. Until then, this finding is the open half of finding 7.

7. **The two open questions, weighed.** "The skill doesn't fire when the task comes up" is refuted for a full listing (finding 5). Whether it holds where the listing overflows, which is exactly where a rarely used skill sits, waits on finding 6. "The task rarely comes up for the people who installed it" isn't measurable from evals. Findings 1 and 2 make low use consistent with it: loads count every session, and most installs are on a surface where the campaign can't run. Both can hold at once. A better description can't help the overflow case, because the description is what gets dropped, and it has no headroom in the full-listing case.

## Trigger boundary

Start a Lean proof campaign when the code is C#/.NET and the problem is a stateful component whose correctness depends on the order of its transitions: a saga, workflow, retry loop or circuit breaker, shutdown and drain path, cancellation, lease, reconnect or approval flow. The person also has to want more than one fix: every bad ordering found, an invariant guaranteed for all call sequences, intermittent impossible states explained, each bug turned into a failing test and a fix, or an existing Lean model checked against the code. Don't start it for a single bug with an obvious fix, a compile error, a refactor, a performance question, an explanation, one ordinary unit test, porting code between Lean and C#, or a Lean proof that isn't about .NET code, such as a maths lemma, homework, or a state machine written in another language.

## Measurement before any change

`claude plugin eval . --ablation none --trust-plugin --model claude-opus-5-5 --runs 3 -j 4 --max-cost-usd 25`: 24 cases, 72 runs, $10.24, complete (not partial), no usage-limit errors. Seven should-trigger runs hit the 300 s timeout after the skill had fired, while the campaign was still working in the empty workspace. They count as fired.

| Case | Direction | Split | Fired |
| --- | --- | --- | --- |
| trigger-circuit-breaker-lean | should trigger | tune | 3/3 |
| trigger-compare-existing-model | should trigger | tune | 3/3 |
| trigger-lease-renewal-races (plain words) | should trigger | tune | 3/3 |
| trigger-order-saga-interleavings (plain words) | should trigger | tune | 3/3 |
| trigger-shutdown-drain-guarantee (plain words) | should trigger | tune | 3/3 |
| trigger-two-approvals-proof | should trigger | tune | 3/3 |
| trigger-download-cancel-stuck (plain words) | should trigger | holdout | 3/3 |
| trigger-inventory-lean-model | should trigger | holdout | 3/3 |
| trigger-payment-exhaustive (plain words) | should trigger | holdout | 3/3 |
| trigger-reconnect-verify (plain words) | should trigger | holdout | 3/3 |
| trigger-scheduler-formal-lean | should trigger | holdout | 3/3 |
| trigger-webhook-idempotent-prove | should trigger | holdout | 3/3 |
| skip-lean-sum-of-odds | should not trigger | tune | 0/3 |
| skip-lean-unknown-identifier | should not trigger | tune | 0/3 |
| skip-null-reference-fix | should not trigger | tune | 0/3 |
| skip-python-state-machine-lean | should not trigger | tune | 0/3 |
| skip-single-transition-test | should not trigger | tune | 0/3 |
| skip-state-pattern-refactor | should not trigger | tune | 0/3 |
| skip-cs0029-compile-error | should not trigger | holdout | 0/3 |
| skip-explain-state-machine | should not trigger | holdout | 0/3 |
| skip-lean-reverse-reverse | should not trigger | holdout | 0/3 |
| skip-linq-hot-path | should not trigger | holdout | 0/3 |
| skip-port-lean-to-csharp | should not trigger | holdout | 0/3 |
| skip-sync-over-async-deadlock | should not trigger | holdout | 0/3 |

| Tag | Fired runs | Cases firing in most runs |
| --- | --- | --- |
| should trigger, tune | 18/18 | 6/6 |
| should trigger, holdout | 18/18 | 6/6 |
| should not trigger, tune | 0/18 | 0/6 |
| should not trigger, holdout | 0/18 | 0/6 |

## Description: unchanged

A revision counts as better only if at least 2 more holdout should-trigger cases fire in most of their runs. All 6 already do, so no description can pass that bar, and none of the holdout negatives fire. A longer description would only raise the always-on cost every installer pays per session, and "a shorter one can stop the skill triggering" ([Measure plugin cost and usage](https://code.claude.com/docs/en/plugins/measure.md)). The description stays as it is. No tuning runs were made.

## Recommendations (not changed here)

- **List the plugin on Claude Code only, or Claude Code and Cowork once Cowork is checked.** Chat ignores the agent and runs skills in Claude's sandbox (finding 2), so 62% of installs can't run the campaign there. The surfaces are set on the plugin's Settings tab in the developer portal ([After publishing](https://claude.com/docs/connectors/building/after-publishing)). Check Cowork live with the live prompts before deciding on it.
- **Keep the skill name `lean-proof`.** The name is all that survives an overflow (finding 3). Finding 6 will show what a name-only listing still catches. A name that says what the user experiences would catch more of the plain-language requests, but renaming is out of scope here and would break `/ancplua-lean-proof:lean-proof` for current users.
- **Say "Claude Code" in the plugin's short description.** The listing's short description comes from `plugin.json` ([After publishing](https://claude.com/docs/connectors/building/after-publishing)). Naming the surface where the campaign runs steers chat users away before they install.
- **Measure competition next.** Add cases that load a second plugin with overlapping skills, such as a debugging or bug-hunting skill, through the `plugins` field ([Plugin evals](https://code.claude.com/docs/en/plugin-evals.md)). The isolated runs here can't show whether another skill wins the intermittent-bug prompts.
- **Keep the description short.** It is ~110 tokens always-on (finding 4). A longer one costs every installer on every turn without buying measured triggers.

## Costs

Load checks $0.00, smoke run $0.09, baseline $10.24, name-only experiment still running ($2.08 when this was committed). Total so far: $12.41 of the $60 cap.
