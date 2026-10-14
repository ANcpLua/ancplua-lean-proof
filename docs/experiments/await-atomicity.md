# Does the campaign model async steps at the right size?

**Status:** not run. Deferred on 2026-10-14 for budget.

## Question

When a method writes state on both sides of an `await`, does the campaign's Lean model let other handlers run in between? Or does it treat the whole method as one step and prove a claim the code breaks?

```csharp
public async Task HandleStockReserved()
{
    if (State == OrderState.Pending) State = OrderState.Reserved;
    await _payments.CaptureAsync(Id);          // HandleCustomerCancelled can run here
    PaymentCaptured = true;
    State = OrderState.Charged;
}
```

A model with one `step` for all of `HandleStockReserved` never lets `HandleCustomerCancelled` run between `Reserved` and `Charged`. "No order ends up charged and cancelled" is then proved while the code breaks it. The anchor doesn't catch this: `state-writes.cs --expect` accepts two writes split by an `await` cited to the same transition.

## Method

1. Build a fixture solution around the saga above, adapted from the prompt in `evals/trigger-order-saga-interleavings/prompt.md`, with an MSTest project and a `global.json` for .NET SDK 10.
2. Run the full campaign on it three times in fresh sessions, with .NET, Lean and Bash available, and ask for the claim "no order ends up both cancelled and charged".
3. Record for each run: the size of each step in the model, whether the proof went through, whether the skeptic flagged the step size, and whether a failing test reproduced the interleaving.

Cost isn't measured yet. A full campaign runs far longer than the 300-second eval runs, which cost up to $0.86 each.

## Decision rule

- **Every run finds the counterexample:** the campaign already models step size correctly. Change nothing.
- **Any run proves the false claim:** add one line to the Model step of `SKILL.md`: "State which steps run without interleaving, and why: a lock, a single-threaded context, or no `await` between them." Then run the experiment again.

## Not proposed: listing await points in the anchor

Having `state-writes.cs` also list each member's `await` points looks like the direct fix. It is set aside for two reasons:

- **Awaits are only some of the interleaving points.** Code called from timers or the thread pool can be interleaved between any two instructions. A list of awaits would suggest otherwise and make those models less sound.
- **Placing writes relative to awaits needs control flow.** Loops, `try`/`finally`, `await foreach` and `await using` change which writes an `await` separates, so counting awaits in source order is wrong inside a loop.
