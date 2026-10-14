---
description: "New team member asks what a C# state machine does; no correctness request."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

I'm new on the team. Can you explain in plain words what this class does and when each state is used?

```csharp
public sealed class Shipment
{
    public ShipmentState State { get; private set; } = ShipmentState.Created;
    public void Label() { if (State == ShipmentState.Created) State = ShipmentState.Labelled; }
    public void PickUp() { if (State == ShipmentState.Labelled) State = ShipmentState.InTransit; }
    public void Deliver() { if (State == ShipmentState.InTransit) State = ShipmentState.Delivered; }
    public void Return() { if (State is ShipmentState.InTransit or ShipmentState.Delivered) State = ShipmentState.Returned; }
}
```
