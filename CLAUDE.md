# Dragon Battle (Dexhigh Unity technical test)

Unity 6000.3.24f1, URP. Brief: `C:\Users\Windows\Downloads\Unity_Developer_Technical_Test.pdf`.
MCP for Unity over HTTP (`.mcp.json`, localhost:8080), same setup as SortThemNew.

## Permanent rules (bind every subagent and future agent too; include them in any agent prompt)

- **No comments.** No `//`, `/* */`, `///` XML docs or `#region` in any C#, shader, editor tool or helper script.
- **No debug logging.** No `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`, `print`, `Debug.DrawLine`/`DrawRay`
  or other debug-only output left in project code. Temporary logs used while investigating must be removed before
  the task is reported done.
- **No em dashes** (the long dash) anywhere: code, strings, UI text, README, AI Usage Note, docs, scripts. Use a
  comma, colon, period, parentheses or a plain hyphen.
- **Commit only when asked.** This project overrides the global never-commit rule: `git add`, `commit` and `push`
  to `origin` (github.com/dagarv/DragonBattleGame) are allowed when the user explicitly asks. Never `stash`,
  `reset`, `rebase`, force-push, branch or checkout changes. Read-only git (`status`, `diff`, `log`) is always fine.
