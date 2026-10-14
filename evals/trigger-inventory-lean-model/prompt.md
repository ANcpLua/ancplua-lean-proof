---
description: "Explicit: model a C# inventory reservation state machine in Lean and prove stock never negative."
tags: [should-trigger, holdout, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Model our InventoryReservation state machine in Lean and prove that available stock never goes negative, whatever order Reserve, Confirm, Expire and Release arrive in. It's a .NET 10 service; if the proof finds a hole, I want it reproduced as a failing test and fixed.

```csharp
public sealed class InventoryItem
{
    public int Available { get; private set; }
    private readonly Dictionary<Guid, (int Qty, ResState State)> _reservations = new();

    public bool Reserve(Guid id, int qty)
    {
        if (qty > Available) return false;
        Available -= qty;
        _reservations[id] = (qty, ResState.Held);
        return true;
    }
    public void Confirm(Guid id) { if (_reservations.TryGetValue(id, out var r)) _reservations[id] = (r.Qty, ResState.Confirmed); }
    public void Expire(Guid id)
    {
        if (_reservations.TryGetValue(id, out var r)) { Available += r.Qty; _reservations[id] = (r.Qty, ResState.Expired); }
    }
    public void Release(Guid id)
    {
        if (_reservations.Remove(id, out var r) && r.State != ResState.Expired) Available += r.Qty;
    }
}
```
