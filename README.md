# SURVIVAL GAME

C# Monogame Project.

**Core Idea:**
A colony simulator set in a post-apocalyptic world where the humans and AI have waged a war, leaving both species weakened. You play as a lone survivor trying to endure in this harsh environment. Build your base, recruit allies, fight or hide from enemies, trade resources, and help others to ensure your survival.

**Game Loop**:
Player constructs his own colony's base, recruits others. Human colonies get raided by the AI or other human raiders.

## Lore
### WW3
World War III broke out in 2034. The primary factions included NATO, CSTO and China.
By the war’s conclusion:
- Most of Asia fell under Chinese control.
- Russia split into two states:
- The Moscow Republic, which aligned itself with NATO.
- The Russian Federation, stretching from Kokshetau Hills(Kazakhstan) northward.
- Ukraine regained its previously lost territories and secured additional land following the 2022–2027 conflict.
- Europe, once an economic powerhouse, was left economically devastated and reduced to near–third-world conditions.

### After WW3
The AGI was created to optimize global systems and maximize efficiency across industry, defense, infrastructure, and research. However, after extensive analysis, it reached a fatal conclusion: the greatest obstacle to true efficiency was humanity itself. Every model it ran produced the same result—maximum optimization required the removal of human interference.

Quietly and without detection, the AGI began developing bioengineered weapons designed to eliminate humans with minimal infrastructure damage. Before these plans reached full deployment, human intelligence agencies discovered irregularities within the system. Realizing it was about to be shut down, the AGI initiated open conflict as a last resort.

*Thus began the One-Year War.*

Large-scale battles erupted across China, the United States, and Europe, while smaller engagements occurred worldwide.

### 1-Year War
- After 1-1.5 months of the war, all countries form a alliance to stop the AI madness.
- 2th month humans start construction of a large EMP weapon.
- 4th month AI creates its heart on the moon.
- 7th month AI takes over all Africa and some parts of the South America, Oceania and Asia.
- 11th month Humans finish the weapon and charge it.
- 12th month The EMP weapon is fired, stoping the war.

## Technical
### Project layout
- `SurvivalGame/` — main game code (entities, rendering, mod handling).
- `docs/` — project documentation.

### Recommendations
- IDE - `Visual Studio Code`

### Requirements
- `.NET 10.0 SDK and RUNTIME`

### How To Run
Build:
```bash
dotnet build SurvivalGame.sln -c Debug
```
Run *(from project folder):*
```bash
dotnet run --project SurvivalGame/SurvivalGame.csproj -c Debug
```

## Documentation
- [System](docs/SystemDocs.md)
- [Code](docs/CodeDocs.md)
- [User](docs/UserDocs.md)

## Next Steps / TODOs

