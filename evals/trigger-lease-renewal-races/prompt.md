---
description: "Distributed lease renewal written from a timer and Dispose; user wants every race found and reproduced."
tags: [should-trigger, tune, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Find all the race conditions in this lease renewal state machine. The lease state is written from the renewal timer callback, from Release and from Dispose, and we've seen two nodes both believe they hold the lease. Before anyone fixes anything I want a reproducing MSTest for each problem you find, then a fix for each.

```csharp
public sealed class LeaseHolder : IAsyncDisposable
{
    private LeaseState _state = LeaseState.NotHeld;
    private DateTimeOffset _expiresAt;
    private readonly Timer _timer;

    public LeaseHolder(ILeaseStore store) { _store = store; _timer = new Timer(_ => _ = RenewAsync(), null, Timeout.Infinite, Timeout.Infinite); }

    public async Task<bool> AcquireAsync()
    {
        if (_state != LeaseState.NotHeld) return _state == LeaseState.Held;
        _state = LeaseState.Acquiring;
        var ok = await _store.TryAcquireAsync(_id, TimeSpan.FromSeconds(30));
        _state = ok ? LeaseState.Held : LeaseState.NotHeld;
        if (ok) { _expiresAt = DateTimeOffset.UtcNow.AddSeconds(30); _timer.Change(10_000, 10_000); }
        return ok;
    }

    private async Task RenewAsync()
    {
        if (_state != LeaseState.Held) return;
        var ok = await _store.TryRenewAsync(_id);
        if (ok) _expiresAt = DateTimeOffset.UtcNow.AddSeconds(30); else _state = LeaseState.Lost;
    }

    public async Task ReleaseAsync() { _state = LeaseState.Releasing; await _store.ReleaseAsync(_id); _state = LeaseState.NotHeld; }
    public async ValueTask DisposeAsync() { _timer.Dispose(); if (_state == LeaseState.Held) await ReleaseAsync(); }
}
```
