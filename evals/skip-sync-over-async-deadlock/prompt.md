---
description: "Classic .Result deadlock in an ASP.NET controller with a known one-line fix."
tags: [should-not-trigger, holdout]
max_turns: 8
allowed_tools: [Read, Glob, Grep, Skill]
---

This controller action hangs forever under load. What's wrong?

```csharp
public IActionResult Get(int id)
{
    var customer = _service.GetCustomerAsync(id).Result;
    return Ok(customer);
}
```
