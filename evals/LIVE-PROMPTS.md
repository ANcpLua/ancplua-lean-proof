# Live prompts

Written before the description was tuned. Try each one in a fresh session from the root of a real C# repository, with this checkout loaded:

```
claude --plugin-dir /Users/alexandernachtmann/RiderProjects/ancplua-lean-proof
```

Replace `<Type>` with a class in that repository whose state is written from more than one method.

1. Should start the campaign, no mention of Lean or proofs:

   > Somewhere in this solution there's a class whose status gets written from more than one async handler, and we've had a few weird production states nobody could reproduce. Find the riskiest one, work out every ordering of those handlers that can leave it in a bad state, and give me a failing MSTest and a fix for each.

2. Should start the campaign, naming Lean:

   > Model the state machine in `<Type>` in Lean 4 and prove its invariants hold for every sequence of calls. Anything the proof breaks should come back as a failing test and a draft PR.

3. Should not start the campaign:

   > `<Type>` has a few nullable warnings and no XML docs on its public methods. Fix the warnings and add the docs, without changing behaviour.

Look for `ancplua-lean-proof:lean-proof` in the transcript (Ctrl+O) after the first reply.
