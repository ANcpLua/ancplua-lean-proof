---
description: "Wants one ordinary MSTest for a single happy-path transition."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Write me an MSTest for this: calling Pay() on a Pending invoice moves it to Paid and sets PaidAt.

```csharp
public sealed class Invoice
{
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;
    public DateTimeOffset? PaidAt { get; private set; }
    public void Pay(TimeProvider clock)
    {
        if (Status != InvoiceStatus.Pending) throw new InvalidOperationException();
        Status = InvoiceStatus.Paid;
        PaidAt = clock.GetUtcNow();
    }
}
```
