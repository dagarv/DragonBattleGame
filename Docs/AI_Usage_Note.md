# Dragon Battle: AI Usage Note

Part of the [Dragon Battle](../README.md) submission for the Dexhigh Services Junior Unity Developer technical
assessment. The same note is also included at the end of the README.

**Tools used**

- **Claude Code** (Anthropic's Claude, in the terminal) as the main assistant.
- **MCP for Unity** (CoplayDev), which connects Claude Code to the running Unity Editor. Through it the assistant
  could create GameObjects, edit prefabs and components, enter Play mode, read the console, take Game view
  screenshots and run the profiler.

**What I used them for**

- **Planning:** turning the brief into eight phases (setup, arena, dragons, controls, abilities, AI, UI, polish),
  and checking which of my downloaded assets covered each requirement. For example, only SoulEater has a real tail
  animation, so it became the player dragon.
- **Coding:** writing the C# scripts phase by phase. I reviewed each script and asked for changes where the
  structure did not suit me, for example keeping one shared `AbilityCaster` for both dragons so the AI could not
  cheat on cooldowns.
- **Editor work:** building the arena, prefabs, animator controller and HUD through MCP, and converting the
  Built-in materials of the asset packs to URP.
- **Debugging and testing:** after each phase the assistant ran the game in Play mode, drove the dragons with a
  temporary editor hook, read the console and checked screenshots. Keyboard feel was checked by playing the game.

**One thing the AI got wrong and how it was fixed**

The first version of the enemy AI used Sky Strike almost all the time. The AI's logic picked Sky Strike whenever it
was ready and the player was out of Fire range, but it did not account for how long the attack takes: the full
take off, fly and land sequence lasts about 9.5 s against a 10 s cooldown. In a test fight the enemy was in the air
for 22 of the first 33 seconds, which made the fight dull and unfair. I found this by watching a test fight and
measuring the time in the air. The fix was a minimum rest time on the ground (8 s since the last landing) before the
AI may use Sky Strike again at mid range, plus a chance to commit to closing in for a Tail Swipe, which it had
otherwise almost never used. After the change the enemy used all three abilities in a varied pattern.

A second, smaller example: the Winner Screen panel came out brown instead of dark blue. The AI had made the panel
slightly transparent and given it a gold outline. In a Linear colour space project, the gold outline copies drawn
behind the panel showed through and warmed its colour. Making the panel fully opaque fixed it.

**How the tools helped**

- The assistant handled the repetitive editor work (placing 121 floor tiles, wiring UI references, creating
  prefab variants), which left more of the 72 hours for gameplay and feel.
- Automated Play mode tests with screenshots caught problems early, like the Sky Strike overuse, a title that did
  not fit the Winner Screen, and damage numbers showing overkill damage.
- I stayed in control of the design: every script was reviewed, and all tuning values are exposed in the Inspector
  so balance can be adjusted without code changes.
