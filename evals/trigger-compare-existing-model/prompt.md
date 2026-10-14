---
description: "Existing Lean model of a C# class has drifted; re-check it against the code."
tags: [should-trigger, tune, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

We wrote a Lean model of our ConnectionLifecycle last quarter, and the C# class has changed since. Can you check whether the model still matches the code and whether the invariants still hold?

The Lean model:

```lean
inductive St | idle | connecting | open | closing | closed
structure Conn where st : St; retries : Nat
def step : Conn → Ev → Conn
  | ⟨.idle, r⟩, .connect => ⟨.connecting, r⟩
  | ⟨.connecting, r⟩, .ok => ⟨.open, 0⟩
  | ⟨.connecting, r⟩, .fail => if r < 3 then ⟨.connecting, r+1⟩ else ⟨.closed, r⟩
  | ⟨.open, r⟩, .close => ⟨.closing, r⟩
  | ⟨.closing, r⟩, .ok => ⟨.closed, r⟩
  | c, _ => c
theorem never_open_after_closed : ∀ c e, c.st = .closed → (step c e).st ≠ .open := by
  intro c e h; cases c; simp_all [step]; cases e <;> simp [step]
```

The current C#:

```csharp
public sealed class ConnectionLifecycle
{
    public ConnState State { get; private set; } = ConnState.Idle;
    private int _retries;
    public void Connect() { if (State is ConnState.Idle or ConnState.Closed) State = ConnState.Connecting; }
    public void OnConnected() { State = ConnState.Open; _retries = 0; }
    public void OnFailed() { State = ++_retries < 5 ? ConnState.Connecting : ConnState.Closed; }
    public void Close() { if (State == ConnState.Open) State = ConnState.Closing; }
    public void OnClosed() => State = ConnState.Closed;
}
```
