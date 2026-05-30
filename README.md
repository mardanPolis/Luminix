# SURVIVAL GAME

C# MonoGame Project.

**Core Idea:**
A RPG colony sim, inspiration taken from games – Rimworld and Skyrim.

**Game Loop**:
Player constructs his own colony's base, recruits others. Explores the world, finds loot, finds other colonies, discovers landmarks.

## Installation

### Setup

Recommended IDE: `VS Code`, [see](https://code.visualstudio.com/download).

Please follow the Monogame [Getting Started Guide](https://docs.monogame.net/articles/getting_started/index.html) on how to setup your OS for development.

Ensure that these are installed:
- `.NET 10.0 SDK and RUNTIME`, [see](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- `GIT`, [see](https://git-scm.com/).

Clone the repository:
```bash
git clone https://github.com/mardanPolis/Monogame.git
```

---

### Build and Run

> [!WARNING]
> Before proceeding, ensure you have created at least one MonoGame project. This initial setup is required to automatically install MonoGame's background tools and dependencies.

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
