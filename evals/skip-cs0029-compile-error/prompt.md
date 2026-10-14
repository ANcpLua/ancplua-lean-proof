---
description: "C# compile error CS0029 with an obvious fix."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<OrderDto>' to 'OrderDto[]'

```csharp
public OrderDto[] GetOrders() => _orders.Select(o => o.ToDto()).ToList();
```
