---
description: "Explicit request to model a C# circuit breaker in Lean 4 and prove a probe invariant."
tags: [should-trigger, tune, names-lean-or-proof]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Can you model this circuit breaker in Lean 4 and prove that in HalfOpen it never lets more than one probe call through, no matter how calls interleave?

```csharp
public sealed class CircuitBreaker
{
    private BreakerState _state = BreakerState.Closed;
    private int _failures;
    private DateTime _openedAt;
    private bool _probeInFlight;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> call)
    {
        if (_state == BreakerState.Open && DateTime.UtcNow - _openedAt > _cooldown)
            _state = BreakerState.HalfOpen;
        if (_state == BreakerState.Open) throw new BrokenCircuitException();
        if (_state == BreakerState.HalfOpen)
        {
            if (_probeInFlight) throw new BrokenCircuitException();
            _probeInFlight = true;
        }
        try
        {
            var result = await call();
            _state = BreakerState.Closed; _failures = 0; _probeInFlight = false;
            return result;
        }
        catch
        {
            if (++_failures >= _threshold || _state == BreakerState.HalfOpen)
            { _state = BreakerState.Open; _openedAt = DateTime.UtcNow; }
            _probeInFlight = false;
            throw;
        }
    }
}
```

It's in our .NET 10 service; any bug the proof turns up should come back as a failing test.
