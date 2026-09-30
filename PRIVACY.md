# Privacy policy

ANcpLua Lean Proof collects no personal data, has no telemetry and shares nothing with its author or anyone else. It has no hooks and no MCP servers, so it sends nothing anywhere on its own.

When you use it, Claude Code runs these on your machine through its normal tools, and you see each command:

- `dotnet run state-writes.cs` downloads two pinned Roslyn packages and their dependencies from nuget.org, reads your project to list the places that write its state, and prints the list on your machine.
- `lean_audit.sh` runs Lake and Lean in your Lean project. `lake build` downloads the dependencies your lakefile declares.
- During a campaign you start, the skill has Claude build and test your code, create git worktrees, and open draft pull requests on your own repository's remote. It publishes or merges only when you say so.

The plugin keeps no data of its own. Everything it writes, such as Lean models, citation files and tests, goes into your repository.

Questions: open an issue at https://github.com/ANcpLua/ancplua-lean-proof/issues.
