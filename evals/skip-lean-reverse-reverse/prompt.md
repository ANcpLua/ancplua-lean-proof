---
description: "Lean 4 homework proof about lists, no .NET code."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

For my course I need to prove that reversing a list twice gives the original list, in Lean 4 without Mathlib. Can you walk me through it?

```lean
theorem rev_rev (xs : List α) : xs.reverse.reverse = xs := by
  sorry
```
