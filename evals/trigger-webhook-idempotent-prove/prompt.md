---
description: "Wants it proved that a C# webhook handler's state transitions are idempotent under redelivery."
tags: [should-trigger, holdout, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Our Stripe-style webhook handler gets the same event delivered more than once and sometimes out of order. Can you prove that its state transitions are idempotent, meaning that replaying any event, in any order, leaves the subscription in the same state as handling it once? If that's not true, show me the sequence as a failing MSTest and fix the handler.

```csharp
public sealed class SubscriptionWebhookHandler
{
    public async Task HandleAsync(WebhookEvent e)
    {
        var sub = await _db.Subscriptions.FindAsync(e.SubscriptionId);
        switch (e.Type)
        {
            case "invoice.paid":
                sub.Status = SubStatus.Active;
                sub.PaidThrough = sub.PaidThrough.AddMonths(1);
                break;
            case "invoice.payment_failed":
                if (sub.Status == SubStatus.Active) sub.Status = SubStatus.PastDue;
                break;
            case "customer.subscription.deleted":
                sub.Status = SubStatus.Cancelled;
                break;
        }
        await _db.SaveChangesAsync();
    }
}
```
