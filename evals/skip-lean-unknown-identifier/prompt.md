---
description: "Lean compile error question, unrelated to any C# code."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Lean says `unknown identifier 'Nat.lt_succ_iff'` on this line after I bumped Mathlib. What's it called now?

```lean
example (a b : Nat) (h : a < b + 1) : a ≤ b := Nat.lt_succ_iff.mp h
```
