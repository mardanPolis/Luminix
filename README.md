# SURVIVAL GAME

C# MonoGame Project.

**Core Idea:**
A colony simulator set in a post-apocalyptic world where the humans and AI have waged a war, leaving both species weakened. You play as a lone survivor trying to endure in this harsh environment. Build your base, recruit allies, fight or hide from enemies, trade resources, and help others to ensure your survival.

**Game Loop**:
Player constructs his own colony's base, recruits others. Human colonies get raided by the AI or other human raiders.

See [Lore](docs/LoreDocs.md) for the complete world background and history.

## Installation

### Setup

Recommended IDE: `VS Code`, see [website](https://code.visualstudio.com/download).

Ensure that these are installed:
- `.NET 10.0 SDK and RUNTIME`, see [website](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- `GIT`, see [website](https://git-scm.com/).

Clone the repository:
```bash
git clone https://github.com/mardanPolis/Monogame.git
```

---

### Build and Run

Open terminal in the project `root folder`.

Build:
```bash
dotnet build SurvivalGame.sln -c Debug
```
Run:
```bash
dotnet run --project SurvivalGame/SurvivalGame.csproj -c Debug
```

## Documentation

See [Guidelines](GUIDELINES.md) for developer guidelines and coding standards.  
See [System](docs/SystemDocs.md) documentation for engine architecture and internal systems.  
See [User](docs/UserDocs.md) documentation for gameplay instructions and user-facing features.

## Next Steps / TODOs
