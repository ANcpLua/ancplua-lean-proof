---
description: "Wants a formal proof that a .NET approval workflow can't publish without two distinct approvals."
tags: [should-trigger, tune, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

I want a formal proof that a document in our .NET approval workflow can never be published without two approvals from two different people, for every sequence of Approve, Reject, Withdraw, Edit and Publish.

```csharp
public sealed class DocumentApproval
{
    private readonly HashSet<Guid> _approvers = new();
    public ApprovalState State { get; private set; } = ApprovalState.Draft;

    public void Submit() { if (State == ApprovalState.Draft) State = ApprovalState.InReview; }
    public void Approve(Guid user)
    {
        if (State != ApprovalState.InReview) return;
        _approvers.Add(user);
        if (_approvers.Count >= 2) State = ApprovalState.Approved;
    }
    public void Reject() { State = ApprovalState.Draft; }
    public void Withdraw(Guid user) { _approvers.Remove(user); }
    public void Edit() { if (State == ApprovalState.Approved) State = ApprovalState.InReview; }
    public void Publish() { if (State == ApprovalState.Approved) State = ApprovalState.Published; }
}
```

Unit tests aren't enough for compliance here. If the proof fails, show me the counterexample as a failing MSTest and fix it.
