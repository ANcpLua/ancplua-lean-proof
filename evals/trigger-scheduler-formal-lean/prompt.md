---
description: "Explicit: use Lean 4 to formally verify a .NET job scheduler state machine."
tags: [should-trigger, holdout, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

I'm tired of whack-a-mole bugs in our job scheduler. Use Lean 4 to formally verify its state machine: a job is never run by two workers at once, and a cancelled job never runs again. This is the core of our .NET worker service:

```csharp
public sealed class JobScheduler
{
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();

    public bool TryClaim(Guid id)
    {
        if (_jobs.TryGetValue(id, out var s) && s == JobState.Queued)
        { _jobs[id] = JobState.Running; return true; }
        return false;
    }
    public void Complete(Guid id) => _jobs[id] = JobState.Done;
    public void Fail(Guid id, int attempt) => _jobs[id] = attempt < 3 ? JobState.Queued : JobState.Dead;
    public void Cancel(Guid id) { if (_jobs.ContainsKey(id)) _jobs[id] = JobState.Cancelled; }
    public void Requeue(Guid id) { if (_jobs[id] != JobState.Running) _jobs[id] = JobState.Queued; }
}
```

Every bug it finds should end up as a failing test and a fix.
