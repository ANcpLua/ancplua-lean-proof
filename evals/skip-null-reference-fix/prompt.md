---
description: "Quick C# NullReferenceException with an obvious one-line fix."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Getting a NullReferenceException on the second line of GetTotal when an order has no discount. Quick fix?

```csharp
public decimal GetTotal(Order order)
{
    var discount = order.Discount.Percentage;
    return order.Lines.Sum(l => l.Price * l.Quantity) * (1 - discount / 100m);
}
```
