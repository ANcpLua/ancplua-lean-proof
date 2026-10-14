---
description: "Payment state machine; wants exhaustive assurance beyond unit tests, bugs as failing tests plus fix PRs."
tags: [should-trigger, holdout, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Before this ships I'd like more than unit tests. Show me exhaustively that Refunded can only ever follow Captured and that no sequence of calls captures twice. Every bug you find should come back as a failing MSTest and a fix in its own PR.

```csharp
public sealed class PaymentIntent
{
    public PaymentStatus Status { get; private set; } = PaymentStatus.Created;
    public decimal Captured { get; private set; }

    public void Authorize() { if (Status == PaymentStatus.Created) Status = PaymentStatus.Authorized; }
    public void Capture(decimal amount)
    {
        if (Status is PaymentStatus.Authorized or PaymentStatus.PartiallyCaptured)
        {
            Captured += amount;
            Status = Captured >= _authorized ? PaymentStatus.Captured : PaymentStatus.PartiallyCaptured;
        }
    }
    public void Refund() { if (Status != PaymentStatus.Created) Status = PaymentStatus.Refunded; }
    public void Void() { if (Status == PaymentStatus.Authorized) Status = PaymentStatus.Voided; }
    public void OnWebhook(string evt)
    {
        if (evt == "charge.succeeded") Capture(_authorized);
        if (evt == "charge.refunded") Refund();
    }
}
```
