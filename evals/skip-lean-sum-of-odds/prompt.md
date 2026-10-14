---
description: "Lean 4 math proof with no .NET code involved."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Help me prove in Lean 4 that the sum of the first n odd numbers is n squared. I've got this far:

```lean
def sumOdd : Nat → Nat
  | 0 => 0
  | n + 1 => sumOdd n + (2 * n + 1)

theorem sumOdd_eq (n : Nat) : sumOdd n = n * n := by
  induction n with
  | zero => rfl
  | succ k ih => sorry
```
