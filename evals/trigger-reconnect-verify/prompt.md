---
description: "WebSocket reconnect loop; systematic verification of two invariants, findings as regression tests."
tags: [should-trigger, holdout, plain-words]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

Can you systematically verify our WebSocket client's reconnect state machine? Two things must hold: we never have two live sockets at once, and we never get stuck in Reconnecting. Anything that breaks either should become a regression test and a fix.

```csharp
public sealed class ResilientSocket
{
    private ClientWebSocket? _socket;
    public SocketState State { get; private set; } = SocketState.Disconnected;

    public async Task ConnectAsync()
    {
        State = SocketState.Connecting;
        var s = new ClientWebSocket();
        await s.ConnectAsync(_uri, CancellationToken.None);
        _socket = s;
        State = SocketState.Connected;
        _ = ReceiveLoopAsync(s);
    }

    private async Task ReceiveLoopAsync(ClientWebSocket s)
    {
        try { while (s.State == WebSocketState.Open) await s.ReceiveAsync(_buffer, CancellationToken.None); }
        catch (WebSocketException) { }
        State = SocketState.Reconnecting;
        await Task.Delay(_backoff.Next());
        await ConnectAsync();
    }

    public async Task CloseAsync()
    {
        State = SocketState.Closing;
        if (_socket is not null) await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
        State = SocketState.Disconnected;
    }
}
```
