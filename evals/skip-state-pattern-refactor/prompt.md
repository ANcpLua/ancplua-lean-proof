---
description: "C# state enum switch; user wants a readability refactor, not correctness assurance."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

This switch is getting long. Can you refactor it into the State pattern, or a transition table, so it's easier to read? Behaviour should stay exactly the same.

```csharp
public void Handle(TicketEvent e)
{
    switch (State)
    {
        case TicketState.Open when e == TicketEvent.Assign: State = TicketState.Assigned; break;
        case TicketState.Assigned when e == TicketEvent.Start: State = TicketState.InProgress; break;
        case TicketState.InProgress when e == TicketEvent.Resolve: State = TicketState.Resolved; break;
        case TicketState.Resolved when e == TicketEvent.Reopen: State = TicketState.Open; break;
        case TicketState.Resolved when e == TicketEvent.Close: State = TicketState.Closed; break;
    }
}
```
