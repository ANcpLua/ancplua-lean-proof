---
description: "Saga ends Cancelled after capture; user wants every interleaving, a failing test and a fix each."
tags: [should-trigger, tune, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Our OrderSaga sometimes ends up Cancelled even though the payment was captured. All our tests pass and we only see it in prod logs a few times a week. Messages for the same order can be handled concurrently.

```csharp
public sealed class OrderSaga
{
    public OrderState State { get; private set; } = OrderState.Pending;
    public bool PaymentCaptured { get; private set; }

    public async Task HandleStockReserved()
    {
        if (State == OrderState.Pending) State = OrderState.Reserved;
        await _payments.CaptureAsync(Id);
        PaymentCaptured = true;
        State = OrderState.Charged;
    }

    public async Task HandleCustomerCancelled()
    {
        if (State is OrderState.Shipped) return;
        State = OrderState.Cancelled;
        if (PaymentCaptured) await _payments.RefundAsync(Id);
    }

    public void HandleShipped()
    {
        if (State == OrderState.Charged) State = OrderState.Shipped;
    }
}
```

I don't want you to patch just the one ordering we happened to see. Find every ordering of these handlers that leaves us charged-but-cancelled (or any other bad end state), give me a failing MSTest for each, and then fix them.
