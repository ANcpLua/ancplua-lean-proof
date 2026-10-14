---
description: "C# performance question about a LINQ hot path."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

This runs for every request and shows up hot in the profiler. How do I make it allocate less?

```csharp
public IReadOnlyList<string> ActiveSkus(IEnumerable<Product> products) =>
    products.Where(p => p.IsActive)
            .Select(p => p.Sku.Trim().ToUpperInvariant())
            .Distinct()
            .OrderBy(s => s)
            .ToList();
```
