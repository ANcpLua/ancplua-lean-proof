---
description: "Background queue shutdown/drain; user wants a guarantee across every ordering, not a patch."
tags: [should-trigger, tune, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

I need a real guarantee about our background queue's shutdown: after StopAsync returns, no enqueued item is lost and none is processed twice, for every ordering of Enqueue, StopAsync and the worker loop. We've had two incidents where an item vanished during a deploy.

```csharp
public sealed class WorkQueue : IHostedService
{
    private readonly Channel<WorkItem> _channel = Channel.CreateUnbounded<WorkItem>();
    private volatile QueueState _state = QueueState.Running;
    private Task? _worker;

    public bool Enqueue(WorkItem item)
    {
        if (_state != QueueState.Running) return false;
        return _channel.Writer.TryWrite(item);
    }

    public Task StartAsync(CancellationToken ct) { _worker = RunAsync(ct); return Task.CompletedTask; }

    public async Task StopAsync(CancellationToken ct)
    {
        _state = QueueState.Draining;
        _channel.Writer.TryComplete();
        if (_worker is not null) await _worker;
        _state = QueueState.Stopped;
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            if (_state == QueueState.Stopped) break;
            await item.ExecuteAsync();
        }
    }
}
```

Don't just eyeball it. Find every way this can break, reproduce each one as a failing test, and fix them.
