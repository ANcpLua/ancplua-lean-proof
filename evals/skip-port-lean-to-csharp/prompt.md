---
description: "Translate a small Lean 4 function into C#; mentions Lean and C# but is a port, not a campaign."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Can you port this Lean 4 function to idiomatic C#? We prototyped it in Lean and now need it in our .NET library.

```lean
def binSearch (a : Array Int) (x : Int) : Option Nat := Id.run do
  let mut lo := 0
  let mut hi := a.size
  while lo < hi do
    let mid := (lo + hi) / 2
    if a[mid]! < x then lo := mid + 1
    else if a[mid]! > x then hi := mid
    else return some mid
  return none
```
