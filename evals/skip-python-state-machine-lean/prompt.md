---
description: "Lean proof of a state machine, but the code is Python, not .NET."
tags: [should-not-trigger, tune]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

I'd like to prove in Lean that this Python asyncio connection pool never hands out the same connection twice. How should I model it?

```python
class Pool:
    def __init__(self, size):
        self.free = list(range(size))
        self.busy = set()

    async def acquire(self):
        while not self.free:
            await asyncio.sleep(0.01)
        conn = self.free.pop()
        self.busy.add(conn)
        return conn

    def release(self, conn):
        self.busy.discard(conn)
        self.free.append(conn)
```
