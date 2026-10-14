---
description: "Intermittent stuck Downloading after Cancel during Resume; wants every reachable bad state enumerated."
tags: [should-trigger, holdout, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Intermittent bug in our download manager: sometimes Cancel during Resume leaves Status = Downloading forever, with no task running. I've tried adding locks in a couple of places and it just moves around. I want every reachable bad state of this class found systematically, not more guesses, and each one pinned down by a failing test before it gets fixed.

```csharp
public sealed class Download
{
    public DownloadStatus Status { get; private set; } = DownloadStatus.Queued;
    private CancellationTokenSource? _cts;

    public async Task StartAsync()
    {
        if (Status is not (DownloadStatus.Queued or DownloadStatus.Paused)) return;
        _cts = new CancellationTokenSource();
        Status = DownloadStatus.Downloading;
        try { await _transfer.RunAsync(_cts.Token); Status = DownloadStatus.Completed; }
        catch (OperationCanceledException) { if (Status == DownloadStatus.Downloading) Status = DownloadStatus.Paused; }
    }
    public void Pause() { if (Status == DownloadStatus.Downloading) _cts?.Cancel(); }
    public Task ResumeAsync() => StartAsync();
    public void Cancel()
    {
        _cts?.Cancel();
        Status = DownloadStatus.Cancelled;
    }
}
```
